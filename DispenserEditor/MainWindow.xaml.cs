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
using DispenserEditor.Models;
using DispenserEditor.ViewModels;

namespace DispenserEditor
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private PathFeature _activeLineFeature = null;
        private PathPoint _draggingPoint = null;
        private bool _isDragging;
        private bool _dragChanged;
        private bool _isDraggingCrosshair;
        private Vector _crosshairDragOffset;
        private bool _crosshairChanged;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            Loaded += OnLoaded;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Recipe.Features.CollectionChanged += OnFeaturesCollectionChanged;
            _viewModel.Recipe.PropertyChanged += OnRecipePropertyChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var feature in _viewModel.Features)
            {
                AttachFeatureHandlers(feature);
            }

            RenderFeatures();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.SelectedFeature))
            {
                RenderFeatures();
            }
            else if (e.PropertyName == nameof(_viewModel.ReferenceImage))
            {
                UpdateCanvasSize();
            }
        }

        private void OnRecipePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipe.OverlayOpacity) ||
                e.PropertyName == nameof(PathRecipe.PixelsPerMillimetre) ||
                e.PropertyName == nameof(PathRecipe.CenterX) ||
                e.PropertyName == nameof(PathRecipe.CenterY))
            {
                RenderFeatures();
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

            RenderFeatures();
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
            RenderFeatures();
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

            RenderFeatures();
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RenderFeatures();
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
            var scale = GetDisplayScale();
            var width = _viewModel.ImageWidth * scale;
            var height = _viewModel.ImageHeight * scale;

            if (width <= 0 || double.IsNaN(width))
            {
                width = ReferenceImage.ActualWidth;
            }

            if (height <= 0 || double.IsNaN(height))
            {
                height = ReferenceImage.ActualHeight;
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
                DrawingCanvas.Width = width;
            }

            if (height > 0 && !double.IsNaN(height))
            {
                DrawingCanvas.Height = height;
            }

            RenderFeatures();
        }

        private void OnReferenceImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();
        }

        private void RenderFeatures()
        {
            DrawingCanvas.Children.Clear();
            var opacity = _viewModel.Recipe.OverlayOpacity;
            var scale = GetScale();
            var origin = GetOrigin(scale);

            DrawCrosshair(origin);

            foreach (var feature in _viewModel.Features)
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
            _viewModel.SetCenter(centerX, centerY, commit: false, updateStatus: false);
        }

        private double GetScale()
        {
            var pixelsPerMillimetre = Math.Max(_viewModel.Recipe.PixelsPerMillimetre, 0.0001);
            var displayScale = GetDisplayScale();
            return Math.Max(pixelsPerMillimetre * displayScale, 0.0001);
        }

        private double GetDisplayScale()
        {
            var displayedWidth = ReferenceImage.ActualWidth;
            var displayedHeight = ReferenceImage.ActualHeight;
            var imageWidth = _viewModel.ImageWidth;
            var imageHeight = _viewModel.ImageHeight;

            double scaleX = double.NaN;
            double scaleY = double.NaN;

            if (!double.IsNaN(displayedWidth) && displayedWidth > 0 && imageWidth > 0)
            {
                scaleX = displayedWidth / imageWidth;
            }

            if (!double.IsNaN(displayedHeight) && displayedHeight > 0 && imageHeight > 0)
            {
                scaleY = displayedHeight / imageHeight;
            }

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

            return 1.0;
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
                if (!ReferenceEquals(child, toggle))
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
            _viewModel.StatusMessage = $"{feature.Name} 추가";
            RenderFeatures();
        }

        private void AppendLinePoint(Point canvasPosition)
        {
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
            _viewModel.StatusMessage = $"{_activeLineFeature.Name} - 점 {_activeLineFeature.Points.Count}";
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
            if (!_isDragging)
            {
                return;
            }

            var position = e.GetPosition(DrawingCanvas);
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
                        _viewModel.StatusMessage = $"센터를 ({_viewModel.AppliedCenterX:F3}, {_viewModel.AppliedCenterY:F3})로 이동했습니다.";
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
            if (_viewModel.CurrentMode == DrawingMode.Line && _activeLineFeature != null)
            {
                if (_activeLineFeature.Points.Count < 2)
                {
                    _viewModel.Features.Remove(_activeLineFeature);
                    _viewModel.SaveSnapshot();
                }

                _activeLineFeature = null;
                _viewModel.StatusMessage = "선 입력 종료";
            }
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
            if (target == null)
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
            _viewModel.StatusMessage = $"{target.Name} 삭제";
        }

        private void OnApplyCenter(object sender, RoutedEventArgs e)
        {
            _viewModel.SetCenter(_viewModel.Recipe.CenterX, _viewModel.Recipe.CenterY);
            RenderFeatures();
        }
    }
}
