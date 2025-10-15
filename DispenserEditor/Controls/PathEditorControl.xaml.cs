using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using DispenserEditor.Models;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Controls
{
    public partial class PathEditorControl : UserControl
    {
        public static readonly DependencyProperty RecipeProperty = DependencyProperty.Register(
            nameof(Recipe),
            typeof(PathRecipe),
            typeof(PathEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRecipeChanged));

        private readonly PathEditorViewModel _viewModel;
        private PathRecipe _currentRecipe;
        private PathFeature _activeLineFeature = null;
        private PathPoint _draggingPoint = null;
        private bool _isDragging;
        private bool _dragChanged;
        private bool _isDraggingCrosshair;
        private Vector _crosshairDragOffset;
        private bool _crosshairChanged;
        private const double DefaultZoom = 1.0;
        private const double MinZoomFactor = 0.1;
        private const double MaxZoomFactor = 10.0;
        private const double ZoomStep = 1.1;
        private double _zoomFactor = DefaultZoom;
        private double _baseDisplayScale = 1.0;
        private bool _isPanning;
        private bool _panMoved;
        private bool _pendingLineCompletion;
        private Point _panStart;
        private Vector _panOffset = new Vector();
        private Vector _panStartOffset;
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1.0, 1.0);
        private readonly TranslateTransform _panTransform = new TranslateTransform();
        private readonly TransformGroup _panZoomTransform;
        private bool _suppressRendering;
        private bool _renderPending;

        public PathRecipe Recipe
        {
            get => (PathRecipe)GetValue(RecipeProperty);
            set => SetValue(RecipeProperty, value);
        }

        public PathEditorViewModel ViewModel => _viewModel;

        public PathEditorControl()
        {
            InitializeComponent();
            _viewModel = new PathEditorViewModel();
            DataContext = _viewModel;

            _panZoomTransform = new TransformGroup();
            _panZoomTransform.Children.Add(_zoomTransform);
            _panZoomTransform.Children.Add(_panTransform);
            PanContent.RenderTransform = _panZoomTransform;
            ApplyPanOffset();

            Loaded += OnLoaded;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            SetCurrentValue(RecipeProperty, _viewModel.Recipe);
        }

        private void RequestRenderFeatures()
        {
            if (_suppressRendering)
            {
                _renderPending = true;
                return;
            }

            _renderPending = false;
            RenderFeatures();
        }

        private void ExecuteWithRenderSuppressed(Action action)
        {
            _suppressRendering = true;
            _renderPending = false;
            try
            {
                action();
            }
            finally
            {
                _suppressRendering = false;
            }

            _renderPending = false;
            RenderFeatures();
        }

        private static void OnRecipeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathEditorControl control)
            {
                control.OnRecipeChanged(e.OldValue as PathRecipe, e.NewValue as PathRecipe);
            }
        }

        private void OnRecipeChanged(PathRecipe oldRecipe, PathRecipe newRecipe)
        {
            if (newRecipe == null)
            {
                var replacement = new PathRecipe();
                SetCurrentValue(RecipeProperty, replacement);
                return;
            }

            if (ReferenceEquals(_currentRecipe, newRecipe))
            {
                return;
            }

            if (_currentRecipe != null)
            {
                DetachRecipeHandlers(_currentRecipe);
            }

            _viewModel.LoadRecipe(newRecipe);
            if (!ReferenceEquals(Recipe, _viewModel.Recipe))
            {
                SetCurrentValue(RecipeProperty, _viewModel.Recipe);
            }
            AttachRecipeHandlers(newRecipe);
            RequestRenderFeatures();
        }

        private void AttachRecipeHandlers(PathRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            _currentRecipe = recipe;
            recipe.Items.CollectionChanged += OnItemsCollectionChanged;
            recipe.PropertyChanged += OnRecipePropertyChanged;

            foreach (var item in recipe.Items)
            {
                AttachItemHandlers(item);
            }
        }

        private void DetachRecipeHandlers(PathRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.Items.CollectionChanged -= OnItemsCollectionChanged;
            recipe.PropertyChanged -= OnRecipePropertyChanged;

            foreach (var item in recipe.Items)
            {
                DetachItemHandlers(item);
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathRecipeItem item in e.OldItems)
                {
                    DetachItemHandlers(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathRecipeItem item in e.NewItems)
                {
                    AttachItemHandlers(item);
                }
            }

            RequestRenderFeatures();
        }

        private void AttachItemHandlers(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged += OnItemPropertyChanged;
            item.Features.CollectionChanged += OnFeaturesCollectionChanged;
            foreach (var feature in item.Features)
            {
                AttachFeatureHandlers(feature);
            }
        }

        private void DetachItemHandlers(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Features.CollectionChanged -= OnFeaturesCollectionChanged;
            foreach (var feature in item.Features)
            {
                DetachFeatureHandlers(feature);
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _viewModel.SelectedItem))
            {
                return;
            }

            if (e.PropertyName == nameof(PathRecipeItem.OverlayOpacity) ||
                e.PropertyName == nameof(PathRecipeItem.PixelsPerMillimetre))
            {
                RequestRenderFeatures();
                return;
            }

            if (e.PropertyName == nameof(PathRecipeItem.CenterX) ||
                e.PropertyName == nameof(PathRecipeItem.CenterY))
            {
                Dispatcher.BeginInvoke(new Action(RequestRenderFeatures), DispatcherPriority.Render);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RequestRenderFeatures();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.SelectedFeature) ||
                e.PropertyName == nameof(_viewModel.SelectedItem))
            {
                RequestRenderFeatures();
            }
            else if (e.PropertyName == nameof(_viewModel.ReferenceImage))
            {
                SetZoom(DefaultZoom, forceUpdate: true);
            }
            else if (e.PropertyName == nameof(PathEditorViewModel.ShowLinePoints))
            {
                RequestRenderFeatures();
            }
        }

        private void OnRecipePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipe.SelectedItem))
            {
                RequestRenderFeatures();
            }
        }

        private void OnFeaturesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathFeature feature in e.OldItems)
                {
                    DetachFeatureHandlers(feature);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathFeature feature in e.NewItems)
                {
                    AttachFeatureHandlers(feature);
                }
            }

            RequestRenderFeatures();
        }

        private void AttachFeatureHandlers(PathFeature feature)
        {
            feature.PropertyChanged += OnFeaturePropertyChanged;
            feature.Points.CollectionChanged += OnPointsCollectionChanged;
            foreach (var point in feature.Points)
            {
                point.PropertyChanged += OnPointPropertyChanged;
            }
        }

        private void DetachFeatureHandlers(PathFeature feature)
        {
            feature.PropertyChanged -= OnFeaturePropertyChanged;
            feature.Points.CollectionChanged -= OnPointsCollectionChanged;
            foreach (var point in feature.Points)
            {
                point.PropertyChanged -= OnPointPropertyChanged;
            }
        }

        private void OnFeaturePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RequestRenderFeatures();
        }

        private void OnPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnPointPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnPointPropertyChanged;
                }
            }

            RequestRenderFeatures();
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RequestRenderFeatures();
        }

        private void OnFeatureGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                _viewModel.SaveSnapshot();
            }
        }

        private void UpdateCanvasSize()
        {
            var calculatedBaseScale = CalculateBaseDisplayScale();
            if (!double.IsNaN(calculatedBaseScale) && calculatedBaseScale > 0)
            {
                _baseDisplayScale = calculatedBaseScale;
            }

            var baseScale = _baseDisplayScale;
            if (double.IsNaN(baseScale) || baseScale <= 0)
            {
                baseScale = 1.0;
            }

            var width = _viewModel.ImageWidth * baseScale;
            var height = _viewModel.ImageHeight * baseScale;

            if ((width <= 0 || double.IsNaN(width)) && _viewModel.ImageWidth > 0)
            {
                width = _viewModel.ImageWidth * _baseDisplayScale;
            }

            if ((height <= 0 || double.IsNaN(height)) && _viewModel.ImageHeight > 0)
            {
                height = _viewModel.ImageHeight * _baseDisplayScale;
            }

            if (width <= 0 || double.IsNaN(width))
            {
                width = CanvasHost.ActualWidth;
            }

            if (height <= 0 || double.IsNaN(height))
            {
                height = CanvasHost.ActualHeight;
            }

            if ((width <= 0 || double.IsNaN(width)) && ReferenceImage.Parent is FrameworkElement parent)
            {
                width = parent.ActualWidth;
            }

            if ((height <= 0 || double.IsNaN(height)) && ReferenceImage.Parent is FrameworkElement parentElement)
            {
                height = parentElement.ActualHeight;
            }

            if (width > 0 && !double.IsNaN(width))
            {
                PanContent.Width = width;
                DrawingCanvas.Width = width;
                ReferenceImage.Width = width;
            }
            else
            {
                PanContent.Width = double.NaN;
                DrawingCanvas.Width = double.NaN;
                ReferenceImage.Width = double.NaN;
            }

            if (height > 0 && !double.IsNaN(height))
            {
                PanContent.Height = height;
                DrawingCanvas.Height = height;
                ReferenceImage.Height = height;
            }
            else
            {
                PanContent.Height = double.NaN;
                DrawingCanvas.Height = double.NaN;
                ReferenceImage.Height = double.NaN;
            }

            var zoomScale = _zoomFactor;
            if (double.IsNaN(zoomScale) || zoomScale <= 0)
            {
                zoomScale = 1.0;
            }

            _zoomTransform.ScaleX = zoomScale;
            _zoomTransform.ScaleY = zoomScale;

            ApplyPanOffset();
            RenderFeatures();
        }

        private void OnReferenceImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();
        }

        private void OnCanvasHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            var previousZoom = _zoomFactor;
            var factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            SetZoom(_zoomFactor * factor);

            if (Math.Abs(_zoomFactor - previousZoom) > 0.0001)
            {
                _viewModel.StatusMessage = $"Zoom: {_zoomFactor * 100:0}%";
            }

            e.Handled = true;
        }

        private void OnResetZoom(object sender, RoutedEventArgs e)
        {
            var previousZoom = _zoomFactor;
            SetZoom(DefaultZoom, forceUpdate: true);

            if (Math.Abs(_zoomFactor - previousZoom) > 0.0001)
            {
                _viewModel.StatusMessage = "Zoom reset to default.";
            }
        }

        private void SetZoom(double zoom, bool forceUpdate = false)
        {
            if (double.IsNaN(zoom))
            {
                return;
            }

            zoom = Math.Max(MinZoomFactor, Math.Min(MaxZoomFactor, zoom));

            if (!forceUpdate && Math.Abs(zoom - _zoomFactor) < 0.0001)
            {
                return;
            }

            _zoomFactor = zoom;
            if (forceUpdate)
            {
                ResetPan();
            }
            UpdateCanvasSize();
        }

        private void RenderFeatures()
        {
            DrawingCanvas.Children.Clear();
            var opacity = _viewModel.SelectedItem?.OverlayOpacity ?? 1.0;
            var scale = GetScale();
            var origin = GetOrigin(scale);

            DrawCrosshair(origin);

            var features = _viewModel.Features;
            if (features == null)
            {
                return;
            }

            var showLinePoints = _viewModel.ShowLinePoints;

            foreach (var feature in features)
            {
                var strokeBrush = feature.IsSelected ? Brushes.DeepSkyBlue : Brushes.OrangeRed;
                strokeBrush = strokeBrush.Clone();
                strokeBrush.Opacity = opacity;

                if (feature.Type == PathFeatureType.Line && feature.Points.Count >= 2)
                {
                    var polyline = new Polyline
                    {
                        Stroke = strokeBrush,
                        StrokeThickness = 2,
                        SnapsToDevicePixels = true
                    };

                    foreach (var point in feature.Points)
                    {
                        var canvasPoint = ToCanvas(point);
                        polyline.Points.Add(canvasPoint);
                    }

                    DrawingCanvas.Children.Add(polyline);
                }

                var showFeaturePoints = feature.Type != PathFeatureType.Line || showLinePoints;

                if (!showFeaturePoints)
                {
                    continue;
                }

                foreach (var point in feature.Points)
                {
                    var canvasPoint = ToCanvas(point);
                    var ellipse = new Ellipse
                    {
                        Width = feature.IsSelected ? 14 : 10,
                        Height = feature.IsSelected ? 14 : 10,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Fill = feature.IsSelected ? Brushes.LightSkyBlue : Brushes.Gold,
                        Opacity = opacity
                    };

                    Canvas.SetLeft(ellipse, canvasPoint.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, canvasPoint.Y - ellipse.Height / 2);
                    DrawingCanvas.Children.Add(ellipse);
                }
            }
        }

        private Point ToCanvas(PathPoint point)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            var x = origin.X + point.X * scale;
            var y = origin.Y + point.Y * scale;
            return new Point(x, y);
        }

        private Point ToModel(Point canvasPoint)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            var x = (canvasPoint.X - origin.X) / scale;
            var y = (canvasPoint.Y - origin.Y) / scale;
            return new Point(Math.Round(x, 3), Math.Round(y, 3));
        }

        private void DrawCrosshair(Point origin)
        {
            var canvasWidth = GetCanvasWidth();
            var canvasHeight = GetCanvasHeight();

            var vertical = new Line
            {
                X1 = origin.X,
                X2 = origin.X,
                Y1 = 0,
                Y2 = canvasHeight,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                IsHitTestVisible = false
            };

            var horizontal = new Line
            {
                Y1 = origin.Y,
                Y2 = origin.Y,
                X1 = 0,
                X2 = canvasWidth,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                IsHitTestVisible = false
            };

            DrawingCanvas.Children.Add(vertical);
            DrawingCanvas.Children.Add(horizontal);
        }

        private bool TryStartCrosshairDrag(Point canvasPosition)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            const double threshold = 10;

            var distanceToVertical = Math.Abs(canvasPosition.X - origin.X);
            var distanceToHorizontal = Math.Abs(canvasPosition.Y - origin.Y);

            if (distanceToVertical <= threshold || distanceToHorizontal <= threshold)
            {
                _isDraggingCrosshair = true;
                _crosshairDragOffset = canvasPosition - origin;
                return true;
            }

            return false;
        }

        private void DragCrosshair(Point canvasPosition)
        {
            var targetOrigin = canvasPosition - _crosshairDragOffset;
            var scale = GetScale();
            var canvasCenter = new Point(GetCanvasWidth() / 2, GetCanvasHeight() / 2);
            var centerX = (canvasCenter.X - targetOrigin.X) / scale;
            var centerY = (canvasCenter.Y - targetOrigin.Y) / scale;

            if (Math.Abs(centerX - _viewModel.AppliedCenterX) < double.Epsilon &&
                Math.Abs(centerY - _viewModel.AppliedCenterY) < double.Epsilon)
            {
                return;
            }

            _crosshairChanged = true;
            if (_viewModel.CrosshairMode == CrosshairMoveMode.KeepPointsFixed)
            {
                ExecuteWithRenderSuppressed(() =>
                    _viewModel.SetCenter(centerX, centerY, commit: false, updateStatus: false));
            }
            else
            {
                _viewModel.SetCenter(centerX, centerY, commit: false, updateStatus: false);
            }
        }

        private double GetScale()
        {
            var pixelsPerMillimetre = Math.Max(_viewModel.SelectedItem?.PixelsPerMillimetre ?? 1.0, 0.0001);
            var displayScale = GetDisplayScale();
            return Math.Max(pixelsPerMillimetre * displayScale, 0.0001);
        }

        private double GetDisplayScale()
        {
            var scale = _baseDisplayScale;
            if (double.IsNaN(scale) || scale <= 0)
            {
                return 1.0;
            }

            return scale;
        }

        private double CalculateBaseDisplayScale()
        {
            var imageWidth = _viewModel.ImageWidth;
            var imageHeight = _viewModel.ImageHeight;

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return double.NaN;
            }

            double availableWidth = CanvasHost.ActualWidth;
            double availableHeight = CanvasHost.ActualHeight;

            if ((availableWidth <= 0 || double.IsNaN(availableWidth)) && CanvasHost.Parent is FrameworkElement parent)
            {
                availableWidth = parent.ActualWidth;
            }

            if ((availableHeight <= 0 || double.IsNaN(availableHeight)) && CanvasHost.Parent is FrameworkElement parentElement)
            {
                availableHeight = parentElement.ActualHeight;
            }

            double scaleX = (!double.IsNaN(availableWidth) && availableWidth > 0) ? availableWidth / imageWidth : double.NaN;
            double scaleY = (!double.IsNaN(availableHeight) && availableHeight > 0) ? availableHeight / imageHeight : double.NaN;

            if (!double.IsNaN(scaleX) && scaleX > 0)
            {
                if (!double.IsNaN(scaleY) && scaleY > 0)
                {
                    return Math.Min(scaleX, scaleY);
                }

                return scaleX;
            }

            if (!double.IsNaN(scaleY) && scaleY > 0)
            {
                return scaleY;
            }

            return double.NaN;
        }

        private Point GetOrigin(double scale)
        {
            var x = GetCanvasWidth() / 2 - _viewModel.AppliedCenterX * scale;
            var y = GetCanvasHeight() / 2 - _viewModel.AppliedCenterY * scale;
            return new Point(x, y);
        }

        private double GetCanvasWidth()
        {
            var width = DrawingCanvas.Width;
            if (width > 0 && !double.IsNaN(width))
            {
                return width;
            }

            width = DrawingCanvas.ActualWidth;
            if (width > 0 && !double.IsNaN(width))
            {
                return width;
            }

            width = ReferenceImage.ActualWidth;
            if (width > 0 && !double.IsNaN(width))
            {
                return width;
            }

            if (ReferenceImage.Parent is FrameworkElement parent)
            {
                width = parent.ActualWidth;
            }

            return width;
        }

        private double GetCanvasHeight()
        {
            var height = DrawingCanvas.Height;
            if (height > 0 && !double.IsNaN(height))
            {
                return height;
            }

            height = DrawingCanvas.ActualHeight;
            if (height > 0 && !double.IsNaN(height))
            {
                return height;
            }

            height = ReferenceImage.ActualHeight;
            if (height > 0 && !double.IsNaN(height))
            {
                return height;
            }

            if (ReferenceImage.Parent is FrameworkElement parent)
            {
                height = parent.ActualHeight;
            }

            return height;
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;
            if (toggle == null)
            {
                return;
            }

            foreach (var child in MainToolBar.Items.OfType<ToggleButton>())
            {
                if (ReferenceEquals(child, toggle))
                {
                    continue;
                }

                if (child.Tag is string childTag && Enum.TryParse(childTag, out DrawingMode _))
                {
                    child.IsChecked = false;
                }
            }

            if (toggle.Tag is string tag && Enum.TryParse(tag, out DrawingMode mode))
            {
                _viewModel?.UpdateMode(mode);
                if (mode != DrawingMode.Line)
                {
                    _activeLineFeature = null;
                }
            }
        }

        private bool TryBeginCrosshairDrag(Point canvasPosition)
        {
            _isDraggingCrosshair = false;
            _crosshairChanged = false;
            if (_viewModel.CurrentMode != DrawingMode.Move)
            {
                return false;
            }
            if (!TryStartCrosshairDrag(canvasPosition))
            {
                return false;
            }

            _draggingPoint = null;
            _isDragging = true;
            _dragChanged = false;
            DrawingCanvas.CaptureMouse();
            return true;
        }

        private void OnCanvasLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(DrawingCanvas);
            if (TryBeginCrosshairDrag(position))
            {
                return;
            }

            switch (_viewModel.CurrentMode)
            {
                case DrawingMode.Point:
                    CreatePointFeature(position);
                    break;
                case DrawingMode.Line:
                    AppendLinePoint(position);
                    break;
                default:
                    BeginDrag(position);
                    break;
            }
        }

        private void CreatePointFeature(Point canvasPosition)
        {
            if (_viewModel.Features == null)
            {
                return;
            }

            var modelPoint = ToModel(canvasPosition);
            var feature = new PathFeature
            {
                Type = PathFeatureType.Point,
                Name = $"Point {_viewModel.Features.Count(f => f.Type == PathFeatureType.Point) + 1}"
            };

            feature.Points.Add(new PathPoint
            {
                X = modelPoint.X,
                Y = modelPoint.Y
            });

            _viewModel.Features.Add(feature);
            _viewModel.SelectedFeature = feature;
            _viewModel.SelectedPoint = feature.Points.First();
            _viewModel.SaveSnapshot();
            _viewModel.StatusMessage = $"Added {feature.Name}.";
            RenderFeatures();
        }

        private void AppendLinePoint(Point canvasPosition)
        {
            if (_viewModel.Features == null)
            {
                return;
            }

            var modelPoint = ToModel(canvasPosition);
            if (_activeLineFeature == null)
            {
                _activeLineFeature = new PathFeature
                {
                    Type = PathFeatureType.Line,
                    Name = $"Line {_viewModel.Features.Count(f => f.Type == PathFeatureType.Line) + 1}"
                };

                _viewModel.Features.Add(_activeLineFeature);
                _viewModel.SelectedFeature = _activeLineFeature;
            }

            _activeLineFeature.Points.Add(new PathPoint
            {
                X = modelPoint.X,
                Y = modelPoint.Y
            });

            _viewModel.SelectedPoint = _activeLineFeature.Points.Last();
            _viewModel.SaveSnapshot();
            _viewModel.StatusMessage = $"{_activeLineFeature.Name} - Point {_activeLineFeature.Points.Count}";
            RenderFeatures();
        }

        private void BeginDrag(Point canvasPosition)
        {
            _activeLineFeature = null;
            _draggingPoint = null;
            _isDraggingCrosshair = false;
            PathFeature feature;
            PathPoint point;
            if (TryFindPoint(canvasPosition, 12, out feature, out point))
            {
                _viewModel.SelectedFeature = feature;
                _viewModel.SelectedPoint = point;
                _draggingPoint = point;
                _isDragging = true;
                _dragChanged = false;
                DrawingCanvas.CaptureMouse();
            }
            else
            {
                var hitFeature = FindFeature(canvasPosition, 10);
                if (hitFeature != null)
                {
                    _viewModel.SelectedFeature = hitFeature;
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(DrawingCanvas);

            if (_isPanning)
            {
                var hostPosition = e.GetPosition(CanvasHost);
                PanTo(hostPosition);
                UpdateMousePositionIndicator(position);
                return;
            }

            UpdateMousePositionIndicator(position);

            if (!_isDragging)
            {
                return;
            }

            if (_isDraggingCrosshair)
            {
                DragCrosshair(position);
                return;
            }

            if (_draggingPoint == null)
            {
                return;
            }

            var modelPoint = ToModel(position);
            if (Math.Abs(_draggingPoint.X - modelPoint.X) > double.Epsilon ||
                Math.Abs(_draggingPoint.Y - modelPoint.Y) > double.Epsilon)
            {
                _draggingPoint.X = modelPoint.X;
                _draggingPoint.Y = modelPoint.Y;
                _dragChanged = true;
            }
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            MousePositionPopup.Visibility = Visibility.Collapsed;
        }

        private void OnCanvasLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                DrawingCanvas.ReleaseMouseCapture();
                _isDragging = false;
                if (_isDraggingCrosshair)
                {
                    _isDraggingCrosshair = false;
                    if (_crosshairChanged)
                    {
                        _viewModel.SaveSnapshot();
                        _viewModel.StatusMessage = $"Center moved to ({_viewModel.AppliedCenterX:F3}, {_viewModel.AppliedCenterY:F3}).";
                        _crosshairChanged = false;
                    }
                }

                _draggingPoint = null;
                if (_dragChanged)
                {
                    _viewModel.SaveSnapshot();
                    _dragChanged = false;
                }
            }
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pendingLineCompletion = _viewModel.CurrentMode == DrawingMode.Line && _activeLineFeature != null;
            _isPanning = true;
            _panMoved = false;
            _panStart = e.GetPosition(CanvasHost);
            _panStartOffset = _panOffset;
            DrawingCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void OnCanvasRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                DrawingCanvas.ReleaseMouseCapture();
                _isPanning = false;
                if (_pendingLineCompletion && !_panMoved)
                {
                    CompleteLineInput();
                }
            }
            else if (_pendingLineCompletion)
            {
                CompleteLineInput();
            }

            _pendingLineCompletion = false;
            _panMoved = false;
        }

        private bool TryFindPoint(Point canvasPoint, double radius, out PathFeature feature, out PathPoint point)
        {
            feature = null;
            point = null;
            foreach (var candidateFeature in _viewModel.Features)
            {
                foreach (var candidatePoint in candidateFeature.Points)
                {
                    var screen = ToCanvas(candidatePoint);
                    if ((screen - canvasPoint).Length <= radius)
                    {
                        feature = candidateFeature;
                        point = candidatePoint;
                        return true;
                    }
                }
            }

            feature = null;
            point = null;
            return false;
        }

        private PathFeature FindFeature(Point canvasPoint, double threshold)
        {
            foreach (var feature in _viewModel.Features)
            {
                if (feature.Points.Count == 0)
                {
                    continue;
                }

                if (feature.Type == PathFeatureType.Point)
                {
                    var screen = ToCanvas(feature.Points.First());
                    if ((screen - canvasPoint).Length <= threshold)
                    {
                        return feature;
                    }
                }
                else
                {
                    for (int i = 0; i < feature.Points.Count - 1; i++)
                    {
                        var start = feature.Points[i];
                        var end = feature.Points[i + 1];
                        var a = ToCanvas(start);
                        var b = ToCanvas(end);
                        if (DistanceToSegment(canvasPoint, a, b) <= threshold)
                        {
                            return feature;
                        }
                    }
                }
            }

            return null;
        }

        private void UpdateMousePositionIndicator(Point canvasPosition)
        {
            if (MousePositionPopup == null || MousePositionText == null)
            {
                return;
            }

            var canvasWidth = DrawingCanvas.ActualWidth;
            var canvasHeight = DrawingCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0 ||
                double.IsNaN(canvasPosition.X) || double.IsNaN(canvasPosition.Y) ||
                canvasPosition.X < 0 || canvasPosition.Y < 0 ||
                canvasPosition.X > canvasWidth || canvasPosition.Y > canvasHeight)
            {
                MousePositionPopup.Visibility = Visibility.Collapsed;
                return;
            }

            var modelPoint = ToModel(canvasPosition);
            MousePositionText.Text = $"X: {modelPoint.X:F3}  Y: {modelPoint.Y:F3}";

            MousePositionPopup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desiredSize = MousePositionPopup.DesiredSize;
            const double offset = 12;

            var left = canvasPosition.X + offset;
            var top = canvasPosition.Y + offset;

            if (!double.IsNaN(desiredSize.Width) && left + desiredSize.Width > canvasWidth)
            {
                left = canvasPosition.X - desiredSize.Width - offset;
            }

            if (!double.IsNaN(desiredSize.Height) && top + desiredSize.Height > canvasHeight)
            {
                top = canvasPosition.Y - desiredSize.Height - offset;
            }

            if (double.IsNaN(left) || double.IsInfinity(left))
            {
                left = 0;
            }

            if (double.IsNaN(top) || double.IsInfinity(top))
            {
                top = 0;
            }

            Canvas.SetLeft(MousePositionPopup, Math.Max(0, left));
            Canvas.SetTop(MousePositionPopup, Math.Max(0, top));
            MousePositionPopup.Visibility = Visibility.Visible;
        }

        private static double DistanceToSegment(Point p, Point a, Point b)
        {
            var ab = b - a;
            var ap = p - a;
            var magnitudeSquared = ab.X * ab.X + ab.Y * ab.Y;
            if (magnitudeSquared < double.Epsilon)
            {
                return (p - a).Length;
            }

            var t = (ap.X * ab.X + ap.Y * ab.Y) / magnitudeSquared;
            t = Math.Max(0, Math.Min(1, t));
            var projection = new Point(a.X + ab.X * t, a.Y + ab.Y * t);
            return (p - projection).Length;
        }

        private void OnLoadImage(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadReferenceImage();
            UpdateCanvasSize();
        }

        private void OnUndo(object sender, RoutedEventArgs e)
        {
            _viewModel.Undo();
            RenderFeatures();
        }

        private void OnRedo(object sender, RoutedEventArgs e)
        {
            _viewModel.Redo();
            RenderFeatures();
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            _viewModel.ImportRecipe();
            RenderFeatures();
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportRecipe();
        }

        private void OnDeleteFeature(object sender, RoutedEventArgs e)
        {
            var target = _viewModel.SelectedFeature;
            if (target == null || _viewModel.Features == null)
            {
                return;
            }

            if (ReferenceEquals(target, _activeLineFeature))
            {
                _activeLineFeature = null;
            }

            _viewModel.Features.Remove(target);
            _viewModel.SelectedFeature = _viewModel.Features.FirstOrDefault();
            _viewModel.SaveSnapshot();
            _viewModel.StatusMessage = $"Deleted {target.Name}.";
            RenderFeatures();
        }

        private void OnClearFeatures(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Features == null || !_viewModel.Features.Any())
            {
                return;
            }

            _activeLineFeature = null;
            _viewModel.Features.Clear();
            _viewModel.SelectedFeature = null;
            _viewModel.SaveSnapshot();
            _viewModel.StatusMessage = "Cleared all features.";
            RenderFeatures();
        }

        private void OnAddItem(object sender, RoutedEventArgs e)
        {
            _viewModel.AddRecipeItem();
            RequestRenderFeatures();
        }

        private void OnRemoveItem(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedItem();
            RequestRenderFeatures();
        }

        private void OnApplyCenter(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
            {
                return;
            }

            _viewModel.SetCenter(_viewModel.SelectedItem.CenterX, _viewModel.SelectedItem.CenterY);
            RenderFeatures();
        }

        private void ApplyPanOffset()
        {
            _panTransform.X = _panOffset.X;
            _panTransform.Y = _panOffset.Y;
        }

        private void ResetPan()
        {
            _panOffset = new Vector();
            ApplyPanOffset();
        }

        private void PanTo(Point position)
        {
            var delta = position - _panStart;
            if (!_panMoved && delta.Length > 2)
            {
                _panMoved = true;
            }

            var newOffset = _panStartOffset + delta;
            if (!AreVectorsClose(newOffset, _panOffset))
            {
                _panOffset = newOffset;
                ApplyPanOffset();
            }
        }

        private void CompleteLineInput()
        {
            if (_viewModel.CurrentMode == DrawingMode.Line && _activeLineFeature != null)
            {
                if (_activeLineFeature.Points.Count < 2)
                {
                    _viewModel.Features.Remove(_activeLineFeature);
                    _viewModel.SaveSnapshot();
                }

                _activeLineFeature = null;
                _viewModel.StatusMessage = "Completed line input.";
            }
        }

        private static bool AreVectorsClose(Vector a, Vector b)
        {
            return Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Y - b.Y) < 0.01;
        }
    }
}
