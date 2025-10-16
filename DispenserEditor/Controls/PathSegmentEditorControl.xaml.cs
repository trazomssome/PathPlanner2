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

namespace DispenserEditor.Controls
{
    public partial class PathSegmentEditorControl : UserControl
    {
        public static readonly DependencyProperty RecipeProperty = DependencyProperty.Register(
            nameof(Recipe),
            typeof(PathRecipe),
            typeof(PathSegmentEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRecipeChanged));

        private const double DefaultZoom = 1.0;
        private const double ZoomStep = 1.1;
        private const double MinZoomFactor = 0.1;
        private const double MaxZoomFactor = 10.0;

        private readonly PathSegmentEditorViewModel _viewModel;
        private PathRecipe _currentRecipe;
        private bool _isPanning;
        private Point _panStart;
        private Vector _panStartOffset;
        private Vector _panOffset;
        private double _zoomFactor = DefaultZoom;

        public PathSegmentEditorControl()
        {
            InitializeComponent();
            _viewModel = new PathSegmentEditorViewModel();
            DataContext = _viewModel;

            Loaded += OnLoaded;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            CanvasHost.MouseWheel += OnMouseWheel;
            CanvasHost.MouseDown += OnMouseDown;
            CanvasHost.MouseMove += OnMouseMove;
            CanvasHost.MouseUp += OnMouseUp;
            CanvasHost.MouseLeave += OnMouseLeave;

            SetCurrentValue(RecipeProperty, _viewModel.Recipe);
            ResetTransforms();
        }

        public PathSegmentEditorViewModel ViewModel => _viewModel;

        public PathRecipe Recipe
        {
            get => (PathRecipe)GetValue(RecipeProperty);
            set => SetValue(RecipeProperty, value);
        }

        private static void OnRecipeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathSegmentEditorControl control)
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
            RenderSegments();
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

        private void AttachItemHandlers(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged += OnItemPropertyChanged;
            item.Segments.CollectionChanged += OnSegmentsCollectionChanged;

            foreach (var segment in item.Segments)
            {
                segment.PropertyChanged += OnSegmentPropertyChanged;
            }
        }

        private void DetachItemHandlers(PathRecipeItem item)
        {
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            item.Segments.CollectionChanged -= OnSegmentsCollectionChanged;

            foreach (var segment in item.Segments)
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RenderSegments();
            UpdateCrosshair();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathSegmentEditorViewModel.SelectedItem) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.SelectedSegment) ||
                e.PropertyName == nameof(PathSegmentEditorViewModel.ShowLinePoints))
            {
                RenderSegments();
            }
            else if (e.PropertyName == nameof(PathSegmentEditorViewModel.AppliedCenterX) ||
                     e.PropertyName == nameof(PathSegmentEditorViewModel.AppliedCenterY) ||
                     e.PropertyName == nameof(PathSegmentEditorViewModel.ImageWidth) ||
                     e.PropertyName == nameof(PathSegmentEditorViewModel.ImageHeight))
            {
                UpdateCrosshair();
            }
        }

        private void OnRecipePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipe.SelectedItem))
            {
                RenderSegments();
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

            RenderSegments();
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PathRecipeItem.Segments) ||
                e.PropertyName == nameof(PathRecipeItem.CenterX) ||
                e.PropertyName == nameof(PathRecipeItem.CenterY))
            {
                RenderSegments();
            }
        }

        private void OnSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PathSegment segment in e.OldItems)
                {
                    segment.PropertyChanged -= OnSegmentPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PathSegment segment in e.NewItems)
                {
                    segment.PropertyChanged += OnSegmentPropertyChanged;
                }
            }

            RenderSegments();
        }

        private void OnSegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RenderSegments();
        }

        private void RenderSegments()
        {
            OverlayCanvas.Children.Clear();

            var segments = _viewModel.Segments;
            if (segments == null || segments.Count == 0)
            {
                return;
            }

            PathSegment previousLineSegment = null;
            foreach (var segment in segments)
            {
                if (segment.SegmentType == SegmentType.Line)
                {
                    if (previousLineSegment != null && previousLineSegment.LineGroup == segment.LineGroup)
                    {
                        var line = new Line
                        {
                            Stroke = Brushes.Lime,
                            StrokeThickness = 2.0,
                            X1 = previousLineSegment.X,
                            Y1 = previousLineSegment.Y,
                            X2 = segment.X,
                            Y2 = segment.Y
                        };

                        OverlayCanvas.Children.Add(line);
                    }

                    if (_viewModel.ShowLinePoints)
                    {
                        DrawPoint(segment, Brushes.OrangeRed);
                    }
                    previousLineSegment = segment;
                }
                else
                {
                    DrawPoint(segment, Brushes.DeepSkyBlue);
                    previousLineSegment = null;
                }
            }
        }

        private void DrawPoint(PathSegment segment, Brush brush)
        {
            var ellipse = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, segment.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, segment.Y - ellipse.Height / 2);
            OverlayCanvas.Children.Add(ellipse);
        }

        private void UpdateCrosshair()
        {
            var x = _viewModel.AppliedCenterX;
            var y = _viewModel.AppliedCenterY;

            CrosshairHorizontal.X1 = 0;
            CrosshairHorizontal.Y1 = y;
            CrosshairHorizontal.X2 = _viewModel.ImageWidth;
            CrosshairHorizontal.Y2 = y;

            CrosshairVertical.X1 = x;
            CrosshairVertical.Y1 = 0;
            CrosshairVertical.X2 = x;
            CrosshairVertical.Y2 = _viewModel.ImageHeight;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            SetZoom(_zoomFactor * factor, e.GetPosition(CanvasHost));
            e.Handled = true;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            CanvasHost.Focus();

            if (e.ChangedButton == MouseButton.Right)
            {
                _isPanning = true;
                _panStart = e.GetPosition(CanvasHost);
                _panStartOffset = _panOffset;
                CanvasHost.CaptureMouse();
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                var imagePoint = TranslateToImageCoordinates(e.GetPosition(CanvasHost));

                if (_viewModel.CurrentMode == DrawingMode.Move)
                {
                    _viewModel.AppliedCenterX = imagePoint.X;
                    _viewModel.AppliedCenterY = imagePoint.Y;
                    UpdateCrosshair();
                }
                else
                {
                    var segment = new PathSegment
                    {
                        SegmentType = _viewModel.CurrentMode == DrawingMode.Line ? SegmentType.Line : SegmentType.Point,
                        LineGroup = _viewModel.Segments?.LastOrDefault()?.LineGroup ?? 0,
                        X = imagePoint.X,
                        Y = imagePoint.Y
                    };

                    _viewModel.AddSegment(segment);
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var current = e.GetPosition(CanvasHost);
                var delta = current - _panStart;
                _panOffset = _panStartOffset + (Vector)delta;
                ApplyTransforms();
                return;
            }

            var imagePoint = TranslateToImageCoordinates(e.GetPosition(CanvasHost));
            _viewModel.AppliedCenterX = imagePoint.X;
            _viewModel.AppliedCenterY = imagePoint.Y;
            UpdateCrosshair();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning && e.ChangedButton == MouseButton.Right)
            {
                _isPanning = false;
                CanvasHost.ReleaseMouseCapture();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                CanvasHost.ReleaseMouseCapture();
            }
        }

        private void SetZoom(double factor, Point center)
        {
            factor = Math.Max(MinZoomFactor, Math.Min(MaxZoomFactor, factor));
            var scaleChange = factor / _zoomFactor;
            _zoomFactor = factor;

            var offset = (Vector)center;
            _panOffset = (offset + _panOffset - offset * scaleChange);
            ApplyTransforms();
        }

        private void ResetTransforms()
        {
            _zoomFactor = DefaultZoom;
            _panOffset = new Vector();
            ApplyTransforms();
        }

        private void ApplyTransforms()
        {
            ZoomTransform.ScaleX = _zoomFactor;
            ZoomTransform.ScaleY = _zoomFactor;
            PanTransform.X = _panOffset.X;
            PanTransform.Y = _panOffset.Y;
        }

        private Point TranslateToImageCoordinates(Point point)
        {
            var x = (point.X - _panOffset.X) / _zoomFactor;
            var y = (point.Y - _panOffset.Y) / _zoomFactor;
            var maxX = Math.Max(0, _viewModel.ImageWidth);
            var maxY = Math.Max(0, _viewModel.ImageHeight);
            x = Math.Max(0, Math.Min(maxX, x));
            y = Math.Max(0, Math.Min(maxY, y));
            return new Point(x, y);
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.IsChecked == true && button.Tag is string tag)
            {
                if (button.Parent is ToolBar toolbar)
                {
                    foreach (var child in toolbar.Items.OfType<ToggleButton>())
                    {
                        if (!ReferenceEquals(child, button))
                        {
                            child.IsChecked = false;
                        }
                    }
                }

                if (Enum.TryParse(tag, out DrawingMode mode))
                {
                    _viewModel.CurrentMode = mode;
                }
            }
        }

        private void OnLoadImage(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadImageFromDisk();
            ResetTransforms();
            UpdateCrosshair();
        }

        private void OnAddSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSegment();
            RenderSegments();
        }

        private void OnDuplicateSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.DuplicateSelectedSegment();
            RenderSegments();
        }

        private void OnRemoveSegment(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveSelectedSegment();
            RenderSegments();
        }

        private void OnUndo(object sender, RoutedEventArgs e)
        {
            _viewModel.Undo();
            RenderSegments();
            UpdateCrosshair();
        }

        private void OnRedo(object sender, RoutedEventArgs e)
        {
            _viewModel.Redo();
            RenderSegments();
            UpdateCrosshair();
        }

        private void OnImportRecipe(object sender, RoutedEventArgs e)
        {
            _viewModel.ImportRecipeFromFile();
            RenderSegments();
            UpdateCrosshair();
        }

        private void OnExportRecipe(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportRecipeToFile();
        }

        private void OnSegmentEditBegin(object sender, DataGridBeginningEditEventArgs e)
        {
            _viewModel.SaveSnapshot();
        }

        private void OnSegmentEditEnd(object sender, DataGridCellEditEndingEventArgs e)
        {
            RenderSegments();
            _viewModel.SaveSnapshot();
        }
    }
}
