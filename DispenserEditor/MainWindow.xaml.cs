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
            var width = ReferenceImage.ActualWidth;
            var height = ReferenceImage.ActualHeight;
            if (width <= 0 || double.IsNaN(width))
            {
                width = ((FrameworkElement)ReferenceImage.Parent).ActualWidth;
            }

            if (height <= 0 || double.IsNaN(height))
            {
                height = ((FrameworkElement)ReferenceImage.Parent).ActualHeight;
            }

            DrawingCanvas.Width = width;
            DrawingCanvas.Height = height;
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

        private double GetScale()
        {
            var pixelsPerMillimetre = Math.Max(_viewModel.Recipe.PixelsPerMillimetre, 0.0001);

            var canvasWidth = DrawingCanvas.ActualWidth;
            var canvasHeight = DrawingCanvas.ActualHeight;
            var imageWidth = _viewModel.ImageWidth;
            var imageHeight = _viewModel.ImageHeight;

            var hasCanvasWidth = !double.IsNaN(canvasWidth) && canvasWidth > 0;
            var hasCanvasHeight = !double.IsNaN(canvasHeight) && canvasHeight > 0;
            var hasImageWidth = !double.IsNaN(imageWidth) && imageWidth > 0;
            var hasImageHeight = !double.IsNaN(imageHeight) && imageHeight > 0;

            var scaleFactor = 1.0;

            if (hasCanvasWidth && hasImageWidth && hasCanvasHeight && hasImageHeight)
            {
                var widthScale = canvasWidth / imageWidth;
                var heightScale = canvasHeight / imageHeight;
                if (!double.IsNaN(widthScale) && !double.IsInfinity(widthScale) &&
                    !double.IsNaN(heightScale) && !double.IsInfinity(heightScale))
                {
                    scaleFactor = (widthScale + heightScale) / 2.0;
                }
            }
            else if (hasCanvasWidth && hasImageWidth)
            {
                var widthScale = canvasWidth / imageWidth;
                if (!double.IsNaN(widthScale) && !double.IsInfinity(widthScale))
                {
                    scaleFactor = widthScale;
                }
            }
            else if (hasCanvasHeight && hasImageHeight)
            {
                var heightScale = canvasHeight / imageHeight;
                if (!double.IsNaN(heightScale) && !double.IsInfinity(heightScale))
                {
                    scaleFactor = heightScale;
                }
            }

            return pixelsPerMillimetre * scaleFactor;
        }

        private Point ToCanvas(PathPoint point)
        {
            var scale = GetScale();
            var x = DrawingCanvas.ActualWidth / 2 + point.X * scale;
            var y = DrawingCanvas.ActualHeight / 2 + point.Y * scale;
            return new Point(x, y);
        }

        private Point ToModel(Point canvasPoint)
        {
            var scale = GetScale();
            var x = (canvasPoint.X - DrawingCanvas.ActualWidth / 2) / scale;
            var y = (canvasPoint.Y - DrawingCanvas.ActualHeight / 2) / scale;
            return new Point(Math.Round(x, 3), Math.Round(y, 3));
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

        private void OnCanvasLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(DrawingCanvas);
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
            if (!_isDragging || _draggingPoint == null)
            {
                return;
            }

            var position = e.GetPosition(DrawingCanvas);
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
