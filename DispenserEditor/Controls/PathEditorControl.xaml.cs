using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using PathSegment = DispenserEditor.Models.PathSegment;

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
        private int? _activeLineGroup = null;
        private PathSegment _draggingSegment = null;
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
        private ObservableCollection<PathSegment> _observedSegments = null;
        private readonly HashSet<PathSegment> _attachedSegments = new HashSet<PathSegment>();

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
            RefreshObservedSegments();
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
            RefreshObservedSegments();
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
        }

        private void DetachItemHandlers(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
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
            if (e.PropertyName == nameof(_viewModel.SelectedItem))
            {
                RefreshObservedSegments();
            }

            if (e.PropertyName == nameof(_viewModel.SelectedItem) ||
                e.PropertyName == nameof(_viewModel.SelectedSegment) ||
                e.PropertyName == nameof(PathEditorViewModel.Segments))
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

        private void AttachSegmentHandlers(PathSegment segment)
        {
            if (segment == null || _attachedSegments.Contains(segment))
            {
                return;
            }

            segment.PropertyChanged += OnSegmentPropertyChanged;
            _attachedSegments.Add(segment);
        }

        private void DetachSegmentHandlers(PathSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            if (_attachedSegments.Remove(segment))
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }
        }

        private void OnSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _observedSegments))
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var segment in _attachedSegments.ToList())
                {
                    DetachSegmentHandlers(segment);
                }

                if (_observedSegments != null)
                {
                    foreach (var segment in _observedSegments)
                    {
                        AttachSegmentHandlers(segment);
                    }
                }

                _viewModel.SaveSnapshot();
                RequestRenderFeatures();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (PathSegment segment in e.OldItems)
                {
                    DetachSegmentHandlers(segment);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathSegment segment in e.NewItems)
                {
                    AttachSegmentHandlers(segment);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                _viewModel.SaveSnapshot();
            }

            RequestRenderFeatures();
        }

        private void OnSegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RequestRenderFeatures();
        }

        private void RefreshObservedSegments()
        {
            if (_observedSegments != null)
            {
                _observedSegments.CollectionChanged -= OnSegmentsCollectionChanged;
            }

            foreach (var segment in _attachedSegments.ToList())
            {
                DetachSegmentHandlers(segment);
            }

            _observedSegments = _viewModel.Segments;

            if (_observedSegments != null)
            {
                _observedSegments.CollectionChanged += OnSegmentsCollectionChanged;
                foreach (var segment in _observedSegments)
                {
                    AttachSegmentHandlers(segment);
                }
            }
        }

        private void OnSegmentGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                _viewModel.SaveSnapshot();
            }
        }

        private void OnSegmentGridRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                Dispatcher.BeginInvoke(new Action(_viewModel.SaveSnapshot), DispatcherPriority.Background);
            }
        }

        private void OnClearSegmentsClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.ClearSegments();
        }

        private void OnRemoveSegmentClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.RemoveSelectedSegment();
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

            var segments = _viewModel.Segments;
            if (segments != null && segments.Count > 0)
            {
                DrawSegments(segments, opacity, _viewModel.ShowLinePoints);
            }
        }

        private void DrawSegments(IList<PathSegment> segments, double opacity, bool showLinePoints)
        {
            if (segments == null || segments.Count == 0)
            {
                return;
            }

            var selectedSegment = _viewModel.SelectedSegment;

            var lineBrush = Brushes.MediumSeaGreen.Clone();
            lineBrush.Opacity = opacity;

            var lineGroups = segments
                .Where(segment => segment != null && segment.SegmentType == SegmentType.Line)
                .GroupBy(segment => segment.LineGroup)
                .Where(group => group.Count() >= 2);

            foreach (var group in lineGroups)
            {
                var polyline = new Polyline
                {
                    Stroke = lineBrush,
                    StrokeThickness = 2,
                    SnapsToDevicePixels = true
                };

                foreach (var segment in group)
                {
                    var canvasPoint = ToCanvas(segment);
                    polyline.Points.Add(canvasPoint);
                }

                DrawingCanvas.Children.Add(polyline);
            }

            foreach (var segment in segments.Where(segment => segment != null))
            {
                var canvasPoint = ToCanvas(segment);
                var isSelected = ReferenceEquals(segment, selectedSegment);
                var isLineSegment = segment.SegmentType == SegmentType.Line;

                if (isLineSegment && !showLinePoints)
                {
                    continue;
                }

                var ellipse = new Ellipse
                {
                    Width = isSelected ? 14 : 10,
                    Height = isSelected ? 14 : 10,
                    Stroke = Brushes.DarkSeaGreen,
                    StrokeThickness = 1,
                    Fill = isSelected ? Brushes.LightGreen : Brushes.MediumAquamarine,
                    Opacity = opacity
                };

                Canvas.SetLeft(ellipse, canvasPoint.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, canvasPoint.Y - ellipse.Height / 2);
                DrawingCanvas.Children.Add(ellipse);
            }
        }

        private Point ToCanvas(PathSegment segment)
        {
            return ToCanvas(segment.X, segment.Y);
        }

        private Point ToCanvas(double x, double y)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            var canvasX = origin.X + x * scale;
            var canvasY = origin.Y + y * scale;
            return new Point(canvasX, canvasY);
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
                    CompleteLineInput();
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

            _draggingSegment = null;
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
                    AddPointSegment(position);
                    break;
                case DrawingMode.Line:
                    AppendLineSegment(position);
                    break;
                default:
                    BeginDrag(position);
                    break;
            }
        }

        private void AddPointSegment(Point canvasPosition)
        {
            if (_viewModel.Segments == null)
            {
                return;
            }

            var modelPoint = ToModel(canvasPosition);
            var segment = new PathSegment
            {
                SegmentType = SegmentType.Point,
                X = modelPoint.X,
                Y = modelPoint.Y
            };

            _viewModel.Segments.Add(segment);
            _viewModel.SelectedSegment = segment;
            _viewModel.StatusMessage = $"Added segment {_viewModel.Segments.IndexOf(segment) + 1}.";
            RenderFeatures();
        }

        private void AppendLineSegment(Point canvasPosition)
        {
            if (_viewModel.Segments == null)
            {
                return;
            }

            var modelPoint = ToModel(canvasPosition);
            if (!_activeLineGroup.HasValue)
            {
                _activeLineGroup = GetNextLineGroup();
            }

            var segment = new PathSegment
            {
                SegmentType = SegmentType.Line,
                LineGroup = _activeLineGroup.Value,
                X = modelPoint.X,
                Y = modelPoint.Y
            };

            _viewModel.Segments.Add(segment);
            _viewModel.SelectedSegment = segment;
            var countInGroup = _viewModel.Segments.Count(s => s != null &&
                s.SegmentType == SegmentType.Line &&
                s.LineGroup == _activeLineGroup.Value);
            _viewModel.StatusMessage = $"Line {_activeLineGroup.Value} - Point {countInGroup}";
            RenderFeatures();
        }

        private int GetNextLineGroup()
        {
            if (_viewModel.Segments == null)
            {
                return 1;
            }

            var maxGroup = _viewModel.Segments
                .Where(segment => segment != null && segment.SegmentType == SegmentType.Line)
                .Select(segment => segment.LineGroup)
                .DefaultIfEmpty(0)
                .Max();

            return maxGroup + 1;
        }

        private void BeginDrag(Point canvasPosition)
        {
            CompleteLineInput();
            _draggingSegment = null;
            _isDraggingCrosshair = false;
            if (TryFindSegment(canvasPosition, 12, out var segment))
            {
                _viewModel.SelectedSegment = segment;
                _draggingSegment = segment;
                _isDragging = true;
                _dragChanged = false;
                DrawingCanvas.CaptureMouse();
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

            if (_draggingSegment != null)
            {
                var modelPoint = ToModel(position);
                if (Math.Abs(_draggingSegment.X - modelPoint.X) > double.Epsilon ||
                    Math.Abs(_draggingSegment.Y - modelPoint.Y) > double.Epsilon)
                {
                    _draggingSegment.X = modelPoint.X;
                    _draggingSegment.Y = modelPoint.Y;
                    _dragChanged = true;
                }
                return;
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

                _draggingSegment = null;
                if (_dragChanged)
                {
                    _viewModel.SaveSnapshot();
                    _dragChanged = false;
                }
            }
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pendingLineCompletion = _viewModel.CurrentMode == DrawingMode.Line && _activeLineGroup.HasValue;
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

        private bool TryFindSegment(Point canvasPoint, double radius, out PathSegment segment)
        {
            segment = null;
            if (_viewModel.Segments == null)
            {
                return false;
            }

            foreach (var candidateSegment in _viewModel.Segments)
            {
                if (candidateSegment == null)
                {
                    continue;
                }

                var screen = ToCanvas(candidateSegment);
                if ((screen - canvasPoint).Length <= radius)
                {
                    segment = candidateSegment;
                    return true;
                }
            }

            return false;
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
            if (!_activeLineGroup.HasValue)
            {
                return;
            }

            var groupId = _activeLineGroup.Value;
            var segments = _viewModel.Segments?
                .Where(segment => segment != null && segment.SegmentType == SegmentType.Line && segment.LineGroup == groupId)
                .ToList();

            if (segments == null || segments.Count == 0)
            {
                _activeLineGroup = null;
                return;
            }

            if (segments.Count < 2)
            {
                foreach (var segment in segments)
                {
                    _viewModel.Segments.Remove(segment);
                }

                _viewModel.SaveSnapshot();
            }

            _activeLineGroup = null;
            _viewModel.StatusMessage = "Completed line input.";
        }

        private static bool AreVectorsClose(Vector a, Vector b)
        {
            return Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Y - b.Y) < 0.01;
        }
    }
}
