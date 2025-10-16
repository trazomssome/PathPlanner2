using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DispenserEditor.Models;
using DispenserEditor.ViewModels;

namespace DispenserEditor.Controls
{
    public partial class PathSegmentEditorControl : UserControl
    {
        public static readonly DependencyProperty RecipeProperty = DependencyProperty.Register(
            nameof(Recipe),
            typeof(PathSegmentRecipe),
            typeof(PathSegmentEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRecipeChanged));

        private readonly PathSegmentEditorViewModel _viewModel;
        private PathSegmentRecipe _currentRecipe;
        private PathSegmentRecipeItem _currentItem;
        private readonly List<PathSegment> _attachedSegments = new List<PathSegment>();
        private bool _isPanning;
        private bool _isDraggingCrosshair;
        private Point _panStart;
        private Vector _panOffset = new Vector();
        private Vector _panStartOffset;
        private Vector _crosshairDragOffset;
        private bool _crosshairChanged;
        private const double DefaultZoom = 1.0;
        private const double MinZoomFactor = 0.1;
        private const double MaxZoomFactor = 10.0;
        private const double ZoomStep = 1.1;
        private double _zoomFactor = DefaultZoom;
        private double _baseDisplayScale = 1.0;

        public PathSegmentRecipe Recipe
        {
            get => (PathSegmentRecipe)GetValue(RecipeProperty);
            set => SetValue(RecipeProperty, value);
        }

        public PathSegmentEditorViewModel ViewModel => _viewModel;

        public PathSegmentEditorControl()
        {
            InitializeComponent();
            _viewModel = new PathSegmentEditorViewModel();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SetCurrentValue(RecipeProperty, _viewModel.Recipe);
            UpdateCanvasSize();
            UpdateTransforms();
            RenderSegments();
        }

        private static void OnRecipeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathSegmentEditorControl control)
            {
                control.OnRecipeChanged(e.OldValue as PathSegmentRecipe, e.NewValue as PathSegmentRecipe);
            }
        }

        private void OnRecipeChanged(PathSegmentRecipe oldRecipe, PathSegmentRecipe newRecipe)
        {
            if (newRecipe == null)
            {
                var replacement = new PathSegmentRecipe();
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
            RenderSegments();
        }

        private void AttachRecipeHandlers(PathSegmentRecipe recipe)
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

        private void DetachRecipeHandlers(PathSegmentRecipe recipe)
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

        private void OnRecipePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathSegmentRecipe.SelectedItem))
            {
                AttachToItem(_viewModel.SelectedItem);
                RenderSegments();
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathSegmentRecipeItem item in e.OldItems)
                {
                    DetachItemHandlers(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathSegmentRecipeItem item in e.NewItems)
                {
                    AttachItemHandlers(item);
                }
            }

            RenderSegments();
        }

        private void AttachItemHandlers(PathSegmentRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged += OnItemPropertyChanged;
            item.Segments.CollectionChanged += OnSegmentsCollectionChanged;
            if (ReferenceEquals(item, _viewModel.SelectedItem))
            {
                AttachToItem(item);
            }
        }

        private void DetachItemHandlers(PathSegmentRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Segments.CollectionChanged -= OnSegmentsCollectionChanged;
            if (ReferenceEquals(item, _currentItem))
            {
                DetachSegmentHandlers();
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _viewModel.SelectedItem))
            {
                return;
            }

            if (e.PropertyName == nameof(PathSegmentRecipeItem.CenterX) ||
                e.PropertyName == nameof(PathSegmentRecipeItem.CenterY) ||
                e.PropertyName == nameof(PathSegmentRecipeItem.PixelsPerMillimetre) ||
                e.PropertyName == nameof(PathSegmentRecipeItem.OverlayOpacity))
            {
                RenderSegments();
            }
        }

        private void OnSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _viewModel.SelectedItem?.Segments))
            {
                return;
            }

            if (e.OldItems != null)
            {
                foreach (PathSegment segment in e.OldItems)
                {
                    DetachSegmentHandler(segment);
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathSegment segment in e.NewItems)
                {
                    AttachSegmentHandler(segment);
                }
            }

            RenderSegments();
        }

        private void AttachToItem(PathSegmentRecipeItem item)
        {
            if (ReferenceEquals(_currentItem, item))
            {
                return;
            }

            DetachSegmentHandlers();
            _currentItem = item;

            if (_currentItem == null)
            {
                return;
            }

            foreach (var segment in _currentItem.Segments)
            {
                AttachSegmentHandler(segment);
            }
        }

        private void AttachSegmentHandler(PathSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            segment.PropertyChanged += OnSegmentPropertyChanged;
            _attachedSegments.Add(segment);
        }

        private void DetachSegmentHandler(PathSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            segment.PropertyChanged -= OnSegmentPropertyChanged;
            _attachedSegments.Remove(segment);
        }

        private void DetachSegmentHandlers()
        {
            foreach (var segment in _attachedSegments.ToList())
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }

            _attachedSegments.Clear();
        }

        private void OnSegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RenderSegments();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathSegmentEditorViewModel.SelectedItem) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.Segments) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.SelectedSegment) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.OverlayOpacity) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.AppliedCenterX) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.AppliedCenterY) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.PixelsPerMillimetre))
            {
                AttachToItem(_viewModel.SelectedItem);
                RenderSegments();
            }
            else if (e.PropertyName == nameof(PathSegmentEditorViewModel.ReferenceImage) ||
                     e.PropertyName == nameof(PathSegmentEditorViewModel.ImageWidth) ||
                     e.PropertyName == nameof(PathSegmentEditorViewModel.ImageHeight))
            {
                UpdateBaseDisplayScale();
                UpdateCanvasSize();
                UpdateTransforms();
                RenderSegments();
            }
        }

        private void OnLoadImage(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadReferenceImage();
        }

        private void OnResetImage(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetReferenceImage();
        }

        private void OnAddLineSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSegment(SegmentType.Line);
        }

        private void OnAddPointSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSegment(SegmentType.Point);
        }

        private void OnRemoveSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedSegment();
        }

        private void OnAddItem(object sender, RoutedEventArgs e)
        {
            _viewModel.AddRecipeItem();
        }

        private void OnRemoveItem(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedItem();
        }

        private void OnResetView(object sender, RoutedEventArgs e)
        {
            ResetView();
        }

        private void RenderSegments()
        {
            DrawingCanvas.Children.Clear();

            DrawCrosshair();

            var item = _viewModel.SelectedItem;
            if (item == null)
            {
                return;
            }

            var segments = item.Segments;
            if (segments == null)
            {
                return;
            }

            var opacity = item.OverlayOpacity;
            PathSegment previousLine = null;

            foreach (var segment in segments)
            {
                var canvasPoint = ToCanvas(segment);

                if (segment.SegmentType == SegmentType.Line &&
                    previousLine != null &&
                    previousLine.SegmentType == SegmentType.Line &&
                    previousLine.LineGroup == segment.LineGroup)
                {
                    var previousPoint = ToCanvas(previousLine);
                    var polyline = new Line
                    {
                        X1 = previousPoint.X,
                        Y1 = previousPoint.Y,
                        X2 = canvasPoint.X,
                        Y2 = canvasPoint.Y,
                        Stroke = Brushes.OrangeRed,
                        StrokeThickness = ReferenceEquals(segment, _viewModel.SelectedSegment) || ReferenceEquals(previousLine, _viewModel.SelectedSegment) ? 3 : 2,
                        Opacity = opacity,
                        SnapsToDevicePixels = true
                    };

                    DrawingCanvas.Children.Add(polyline);
                }

                var ellipse = new Ellipse
                {
                    Width = ReferenceEquals(segment, _viewModel.SelectedSegment) ? 14 : 10,
                    Height = ReferenceEquals(segment, _viewModel.SelectedSegment) ? 14 : 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Fill = segment.SegmentType == SegmentType.Line ? Brushes.Gold : Brushes.DeepSkyBlue,
                    Opacity = opacity
                };

                Canvas.SetLeft(ellipse, canvasPoint.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, canvasPoint.Y - ellipse.Height / 2);
                DrawingCanvas.Children.Add(ellipse);

                previousLine = segment.SegmentType == SegmentType.Line ? segment : null;
            }
        }

        private void DrawCrosshair()
        {
            var origin = GetOrigin(GetScale());
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
                X1 = 0,
                X2 = canvasWidth,
                Y1 = origin.Y,
                Y2 = origin.Y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                IsHitTestVisible = false
            };

            DrawingCanvas.Children.Add(vertical);
            DrawingCanvas.Children.Add(horizontal);
        }

        private Point ToCanvas(PathSegment segment)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            var x = origin.X + segment.X * scale;
            var y = origin.Y + segment.Y * scale;
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

            return _viewModel.ImageWidth;
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

            return _viewModel.ImageHeight;
        }

        private void UpdateCanvasSize()
        {
            var displayScale = GetDisplayScale();
            var width = _viewModel.ImageWidth * displayScale;
            var height = _viewModel.ImageHeight * displayScale;

            if (width <= 0 || double.IsNaN(width))
            {
                width = CanvasHost.ActualWidth;
            }

            if (height <= 0 || double.IsNaN(height))
            {
                height = CanvasHost.ActualHeight;
            }

            DrawingCanvas.Width = width;
            DrawingCanvas.Height = height;
            ReferenceImage.Width = width;
            ReferenceImage.Height = height;
        }

        private void UpdateBaseDisplayScale()
        {
            var imageWidth = _viewModel.ImageWidth;
            var imageHeight = _viewModel.ImageHeight;

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                _baseDisplayScale = 1.0;
                return;
            }

            double availableWidth = CanvasHost.ActualWidth;
            double availableHeight = CanvasHost.ActualHeight;

            if (availableWidth <= 0 || double.IsNaN(availableWidth))
            {
                availableWidth = ActualWidth;
            }

            if (availableHeight <= 0 || double.IsNaN(availableHeight))
            {
                availableHeight = ActualHeight;
            }

            var scaleX = (!double.IsNaN(availableWidth) && availableWidth > 0) ? availableWidth / imageWidth : double.NaN;
            var scaleY = (!double.IsNaN(availableHeight) && availableHeight > 0) ? availableHeight / imageHeight : double.NaN;

            if (!double.IsNaN(scaleX) && scaleX > 0)
            {
                if (!double.IsNaN(scaleY) && scaleY > 0)
                {
                    _baseDisplayScale = Math.Min(scaleX, scaleY);
                }
                else
                {
                    _baseDisplayScale = scaleX;
                }
            }
            else if (!double.IsNaN(scaleY) && scaleY > 0)
            {
                _baseDisplayScale = scaleY;
            }
            else
            {
                _baseDisplayScale = 1.0;
            }
        }

        private void UpdateTransforms()
        {
            var zoom = _zoomFactor;
            if (double.IsNaN(zoom) || zoom <= 0)
            {
                zoom = 1.0;
            }

            ZoomTransform.ScaleX = zoom;
            ZoomTransform.ScaleY = zoom;
            PanTransform.X = _panOffset.X;
            PanTransform.Y = _panOffset.Y;
        }

        private void ResetView()
        {
            _zoomFactor = DefaultZoom;
            _panOffset = new Vector();
            UpdateBaseDisplayScale();
            UpdateCanvasSize();
            UpdateTransforms();
            RenderSegments();
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
            {
                return;
            }

            var zoomDelta = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            var newZoom = Math.Max(MinZoomFactor, Math.Min(MaxZoomFactor, _zoomFactor * zoomDelta));

            if (Math.Abs(newZoom - _zoomFactor) < double.Epsilon)
            {
                return;
            }

            _zoomFactor = newZoom;
            UpdateTransforms();
            RenderSegments();
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            var position = e.GetPosition(DrawingCanvas);

            if (e.ChangedButton == MouseButton.Middle)
            {
                var hostPosition = e.GetPosition(CanvasHost);
                StartPan(hostPosition);
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                if (TryStartCrosshairDrag(position))
                {
                    e.Handled = true;
                    return;
                }

                var segment = HitTestSegment(position);
                if (segment != null)
                {
                    _viewModel.SelectedSegment = segment;
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(DrawingCanvas);

            if (_isPanning)
            {
                var hostPosition = e.GetPosition(CanvasHost);
                ContinuePan(hostPosition);
                e.Handled = true;
            }
            else if (_isDraggingCrosshair)
            {
                DragCrosshair(position);
                e.Handled = true;
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                EndPan();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                EndCrosshairDrag();
                e.Handled = true;
            }
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            EndPan();
            EndCrosshairDrag();
        }

        private void StartPan(Point position)
        {
            _isPanning = true;
            _panStart = position;
            _panStartOffset = _panOffset;
            Cursor = Cursors.SizeAll;
        }

        private void ContinuePan(Point position)
        {
            var delta = position - _panStart;
            _panOffset = _panStartOffset + (Vector)delta;
            UpdateTransforms();
            RenderSegments();
        }

        private void EndPan()
        {
            if (!_isPanning)
            {
                return;
            }

            _isPanning = false;
            Cursor = Cursors.Arrow;
        }

        private bool TryStartCrosshairDrag(Point canvasPosition)
        {
            var scale = GetScale();
            var origin = GetOrigin(scale);
            const double threshold = 12;

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
            _viewModel.SetCenter(centerX, centerY);
        }

        private void EndCrosshairDrag()
        {
            if (!_isDraggingCrosshair)
            {
                return;
            }

            _isDraggingCrosshair = false;

            if (_crosshairChanged)
            {
                _crosshairChanged = false;
                RenderSegments();
            }
        }

        private PathSegment HitTestSegment(Point canvasPosition)
        {
            var scale = GetScale();
            var threshold = 10.0;
            PathSegment closest = null;
            double closestDistance = double.MaxValue;

            foreach (var segment in _viewModel.SelectedItem?.Segments ?? Enumerable.Empty<PathSegment>())
            {
                var point = ToCanvas(segment);
                var distance = (point - canvasPosition).Length;
                if (distance < threshold && distance < closestDistance)
                {
                    closest = segment;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void OnCanvasHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBaseDisplayScale();
            UpdateCanvasSize();
            UpdateTransforms();
            RenderSegments();
        }
    }
}
