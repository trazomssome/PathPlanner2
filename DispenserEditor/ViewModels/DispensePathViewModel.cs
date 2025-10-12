using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using DispensePath;

namespace DispenserEditor.ViewModels
{
    public sealed class DispensePathViewModel : ObservableObject
    {
        private readonly ObservableCollection<DispensePolylineViewModel> _polylines;
        private readonly ObservableCollection<DispensePointNodeViewModel> _anchorNodes;

        private readonly RelayCommand _removePolylineCommand;
        private readonly RelayCommand _addPointCommand;
        private readonly RelayCommand _removePointCommand;
        private readonly RelayCommand _flipHorizontalCommand;
        private readonly RelayCommand _flipVerticalCommand;
        private readonly RelayCommand _saveRecipeCommand;
        private readonly RelayCommand _loadRecipeCommand;
        private readonly RelayCommand _addAnchorNodeCommand;
        private readonly RelayCommand _removeAnchorNodeCommand;
        private readonly RelayCommand _clearAnchorNodesCommand;

        private DispensePolylineViewModel _selectedPolyline;
        private DispensePointNodeViewModel _selectedPoint;
        private DispensePointNodeViewModel _selectedAnchorNode;

        private double _pixelSizeX = 0.01;
        private double _pixelSizeY = 0.01;
        private string _backgroundImagePath;
        private ImageSource _backgroundImage;
        private double _canvasWidth = 1200;
        private double _canvasHeight = 800;
        private double _zoom = 1.0;
        private string _recipePath;
        private string _statusMessage;

        public DispensePathViewModel()
        {
            _polylines = new ObservableCollection<DispensePolylineViewModel>();
            _anchorNodes = new ObservableCollection<DispensePointNodeViewModel>();

            _polylines.CollectionChanged += PolylinesOnCollectionChanged;
            _anchorNodes.CollectionChanged += AnchorNodesOnCollectionChanged;

            StatusMessage = "Ready.";

            AddPolylineCommand = new RelayCommand(AddPolyline);
            _removePolylineCommand = new RelayCommand(RemoveSelectedPolyline, _ => SelectedPolyline != null);
            RemovePolylineCommand = _removePolylineCommand;

            _addPointCommand = new RelayCommand(AddPointToSelected, _ => SelectedPolyline != null);
            AddPointCommand = _addPointCommand;
            _removePointCommand = new RelayCommand(RemoveSelectedPoint, _ => SelectedPoint != null);
            RemovePointCommand = _removePointCommand;

            _flipHorizontalCommand = new RelayCommand(_ => SelectedPolyline?.FlipHorizontal(), _ => SelectedPolyline != null);
            _flipVerticalCommand = new RelayCommand(_ => SelectedPolyline?.FlipVertical(), _ => SelectedPolyline != null);
            FlipHorizontalCommand = _flipHorizontalCommand;
            FlipVerticalCommand = _flipVerticalCommand;

            ZoomInCommand = new RelayCommand(() => Zoom *= 1.2);
            ZoomOutCommand = new RelayCommand(() => Zoom /= 1.2);
            ResetZoomCommand = new RelayCommand(() => Zoom = 1.0);

            _saveRecipeCommand = new RelayCommand(SaveRecipe, _ => !string.IsNullOrWhiteSpace(RecipePath));
            _loadRecipeCommand = new RelayCommand(LoadRecipe, _ => !string.IsNullOrWhiteSpace(RecipePath) && File.Exists(RecipePath));
            SaveRecipeCommand = _saveRecipeCommand;
            LoadRecipeCommand = _loadRecipeCommand;

            _addAnchorNodeCommand = new RelayCommand(AddAnchorNodeAtCenter);
            _removeAnchorNodeCommand = new RelayCommand(RemoveSelectedAnchor, _ => SelectedAnchorNode != null);
            _clearAnchorNodesCommand = new RelayCommand(() => AnchorNodes.Clear(), _ => AnchorNodes.Count > 0);
            AddAnchorNodeCommand = _addAnchorNodeCommand;
            RemoveAnchorNodeCommand = _removeAnchorNodeCommand;
            ClearAnchorNodesCommand = _clearAnchorNodesCommand;
        }

        public ObservableCollection<DispensePolylineViewModel> Polylines => _polylines;

        public ObservableCollection<DispensePointNodeViewModel> AnchorNodes => _anchorNodes;

        public DispensePolylineViewModel SelectedPolyline
        {
            get => _selectedPolyline;
            set
            {
                if (SetProperty(ref _selectedPolyline, value))
                {
                    UpdatePolylineSelection();
                    SelectedPoint = value?.Points.FirstOrDefault();
                    _removePolylineCommand.RaiseCanExecuteChanged();
                    _flipHorizontalCommand.RaiseCanExecuteChanged();
                    _flipVerticalCommand.RaiseCanExecuteChanged();
                    _addPointCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DispensePointNodeViewModel SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (SetProperty(ref _selectedPoint, value))
                {
                    UpdatePointSelection();
                    _removePointCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DispensePointNodeViewModel SelectedAnchorNode
        {
            get => _selectedAnchorNode;
            set
            {
                if (SetProperty(ref _selectedAnchorNode, value))
                {
                    _removeAnchorNodeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public double PixelSizeX
        {
            get => _pixelSizeX;
            set => SetProperty(ref _pixelSizeX, value);
        }

        public double PixelSizeY
        {
            get => _pixelSizeY;
            set => SetProperty(ref _pixelSizeY, value);
        }

        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                if (SetProperty(ref _backgroundImagePath, value))
                {
                    UpdateBackgroundImage();
                }
            }
        }

        public ImageSource BackgroundImage
        {
            get => _backgroundImage;
            private set => SetProperty(ref _backgroundImage, value);
        }

        public double CanvasWidth
        {
            get => _canvasWidth;
            private set => SetProperty(ref _canvasWidth, value);
        }

        public double CanvasHeight
        {
            get => _canvasHeight;
            private set => SetProperty(ref _canvasHeight, value);
        }

        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        public string RecipePath
        {
            get => _recipePath;
            set
            {
                if (SetProperty(ref _recipePath, value))
                {
                    _saveRecipeCommand.RaiseCanExecuteChanged();
                    _loadRecipeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ICommand AddPolylineCommand { get; }
        public ICommand RemovePolylineCommand { get; }
        public ICommand AddPointCommand { get; }
        public ICommand RemovePointCommand { get; }
        public ICommand FlipHorizontalCommand { get; }
        public ICommand FlipVerticalCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand SaveRecipeCommand { get; }
        public ICommand LoadRecipeCommand { get; }
        public ICommand AddAnchorNodeCommand { get; }
        public ICommand RemoveAnchorNodeCommand { get; }
        public ICommand ClearAnchorNodesCommand { get; }

        public void AddPointAtPosition(Point position, bool insertAfterSelected)
        {
            if (SelectedPolyline == null)
            {
                AddPolyline();
            }

            if (SelectedPolyline == null)
            {
                return;
            }

            DispensePointNodeViewModel created;
            if (insertAfterSelected && SelectedPoint != null)
            {
                int index = SelectedPolyline.Points.IndexOf(SelectedPoint);
                created = SelectedPolyline.InsertPoint(index + 1, position.X, position.Y);
            }
            else
            {
                created = SelectedPolyline.AddPoint(position.X, position.Y);
            }

            SelectedPoint = created;
        }

        public void AddAnchorNode(Point position)
        {
            var node = new DispensePointNodeViewModel
            {
                X = position.X,
                Y = position.Y
            };
            AnchorNodes.Add(node);
            SelectedAnchorNode = node;
        }

        public void LoadFromRecipe(Recipe_DispensingPoints recipe)
        {
            _polylines.Clear();
            _anchorNodes.Clear();

            if (recipe == null)
            {
                StatusMessage = "Recipe is empty.";
                return;
            }

            PixelSizeX = recipe.PixelSizeX;
            PixelSizeY = recipe.PixelSizeY;
            BackgroundImagePath = recipe.BackgroundImagePath;

            var remainingNodes = recipe.Nodes != null
                ? new List<DispensePointNode>(recipe.Nodes.Select(CloneNode))
                : new List<DispensePointNode>();

            foreach (var poly in recipe.Polylines.OrderBy(p => p.Order))
            {
                var matchedNodes = MatchNodes(poly, remainingNodes);
                var vm = matchedNodes.Count > 0
                    ? new DispensePolylineViewModel(poly, matchedNodes)
                    : new DispensePolylineViewModel(poly);
                _polylines.Add(vm);
            }

            foreach (var node in remainingNodes)
            {
                _anchorNodes.Add(new DispensePointNodeViewModel(node));
            }

            if (_polylines.Count > 0)
            {
                SelectedPolyline = _polylines[0];
                SelectedPoint = SelectedPolyline.Points.FirstOrDefault();
            }
            else
            {
                SelectedPolyline = null;
                SelectedPoint = null;
            }

            StatusMessage = $"Loaded {Polylines.Count} polylines.";
        }

        public void SaveRecipe()
        {
            if (string.IsNullOrWhiteSpace(RecipePath))
            {
                StatusMessage = "Recipe path is empty.";
                return;
            }

            var recipe = BuildRecipe();
            try
            {
                recipe.SaveAs(RecipePath);
                StatusMessage = $"Saved to {RecipePath}.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        public void LoadRecipe()
        {
            if (string.IsNullOrWhiteSpace(RecipePath) || !File.Exists(RecipePath))
            {
                StatusMessage = "Recipe file not found.";
                return;
            }

            Recipe_DispensingPoints recipe;
            try
            {
                recipe = new Recipe_DispensingPoints().LoadAs(RecipePath);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                return;
            }

            LoadFromRecipe(recipe);
        }

        public Recipe_DispensingPoints BuildRecipe()
        {
            var recipe = new Recipe_DispensingPoints
            {
                PixelSizeX = PixelSizeX,
                PixelSizeY = PixelSizeY,
                BackgroundImagePath = BackgroundImagePath
            };

            foreach (var poly in Polylines)
            {
                recipe.Polylines.Add(poly.ToModel());
                foreach (var node in poly.Points)
                {
                    recipe.Nodes.Add(node.ToModel());
                }
            }

            foreach (var node in AnchorNodes)
            {
                recipe.Nodes.Add(node.ToModel());
            }

            return recipe;
        }

        private void AddPolyline()
        {
            var polyline = new DispensePolylineViewModel
            {
                Order = _polylines.Count + 1,
                Use = true,
                OpenTimeMs = 0,
                CloseTimeMs = 0,
                NumOfPulse = 0
            };

            var startX = CanvasWidth / 2.0 - 50;
            var startY = CanvasHeight / 2.0 - 50;
            polyline.AddPoint(startX, startY);
            polyline.AddPoint(startX + 100, startY + 100);

            _polylines.Add(polyline);
            SelectedPolyline = polyline;
            SelectedPoint = polyline.Points.FirstOrDefault();
        }

        private void RemoveSelectedPolyline(object obj)
        {
            if (SelectedPolyline == null)
            {
                return;
            }

            var index = _polylines.IndexOf(SelectedPolyline);
            _polylines.Remove(SelectedPolyline);

            for (int i = 0; i < _polylines.Count; i++)
            {
                _polylines[i].Order = i + 1;
            }

            if (_polylines.Count == 0)
            {
                SelectedPolyline = null;
                SelectedPoint = null;
            }
            else
            {
                var newIndex = Math.Max(0, Math.Min(index, _polylines.Count - 1));
                SelectedPolyline = _polylines[newIndex];
            }
        }

        private void AddPointToSelected(object obj)
        {
            if (SelectedPolyline == null)
            {
                AddPolyline();
                return;
            }

            double x;
            double y;

            if (SelectedPoint != null)
            {
                x = SelectedPoint.X + 25;
                y = SelectedPoint.Y + 25;
            }
            else if (SelectedPolyline.Points.Count > 0)
            {
                var last = SelectedPolyline.Points.Last();
                x = last.X + 25;
                y = last.Y + 25;
            }
            else
            {
                x = CanvasWidth / 2.0;
                y = CanvasHeight / 2.0;
            }

            SelectedPoint = SelectedPolyline.AddPoint(x, y);
        }

        private void RemoveSelectedPoint(object obj)
        {
            if (SelectedPolyline == null || SelectedPoint == null)
            {
                return;
            }

            var index = SelectedPolyline.Points.IndexOf(SelectedPoint);
            SelectedPolyline.RemovePoint(SelectedPoint);

            if (SelectedPolyline.Points.Count == 0)
            {
                SelectedPoint = null;
            }
            else
            {
                var newIndex = Math.Max(0, Math.Min(index - 1, SelectedPolyline.Points.Count - 1));
                SelectedPoint = SelectedPolyline.Points[newIndex];
            }
        }

        private void RemoveSelectedAnchor(object obj)
        {
            if (SelectedAnchorNode == null)
            {
                return;
            }

            var index = AnchorNodes.IndexOf(SelectedAnchorNode);
            AnchorNodes.Remove(SelectedAnchorNode);

            if (AnchorNodes.Count == 0)
            {
                SelectedAnchorNode = null;
            }
            else
            {
                var newIndex = Math.Max(0, Math.Min(index - 1, AnchorNodes.Count - 1));
                SelectedAnchorNode = AnchorNodes[newIndex];
            }
        }

        private void AddAnchorNodeAtCenter(object obj)
        {
            AddAnchorNode(new Point(CanvasWidth / 2.0, CanvasHeight / 2.0));
        }

        private void UpdateBackgroundImage()
        {
            if (string.IsNullOrWhiteSpace(BackgroundImagePath) || !File.Exists(BackgroundImagePath))
            {
                BackgroundImage = null;
                return;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(BackgroundImagePath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                BackgroundImage = image;
                CanvasWidth = image.PixelWidth;
                CanvasHeight = image.PixelHeight;
            }
            catch
            {
                BackgroundImage = null;
            }
        }

        private void UpdatePolylineSelection()
        {
            foreach (var poly in _polylines)
            {
                poly.IsSelected = poly == _selectedPolyline;
            }
        }

        private void UpdatePointSelection()
        {
            foreach (var poly in _polylines)
            {
                foreach (var node in poly.Points)
                {
                    node.IsSelected = node == _selectedPoint;
                }
            }
        }

        private void PolylinesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DispensePolylineViewModel item in e.NewItems)
                {
                    item.PropertyChanged += PolylineOnPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (DispensePolylineViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= PolylineOnPropertyChanged;
                }
            }

            for (int i = 0; i < _polylines.Count; i++)
            {
                _polylines[i].Order = i + 1;
            }
        }

        private void PolylineOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DispensePolylineViewModel.IsSelected))
            {
                if (sender is DispensePolylineViewModel polyline && polyline.IsSelected && polyline != SelectedPolyline)
                {
                    SelectedPolyline = polyline;
                }
            }
        }

        private void AnchorNodesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _clearAnchorNodesCommand.RaiseCanExecuteChanged();
            _removeAnchorNodeCommand.RaiseCanExecuteChanged();
        }

        private static DispensePointNode CloneNode(DispensePointNode node)
        {
            return new DispensePointNode
            {
                Position = new System.Drawing.PointF(node.Position.X, node.Position.Y),
                OffsetZ = node.OffsetZ,
                Speed = node.Speed,
                Use = node.Use
            };
        }

        private static List<DispensePointNode> MatchNodes(DispensePolyline polyline, List<DispensePointNode> candidates)
        {
            var result = new List<DispensePointNode>();

            if (polyline.Points.Count == 0 || candidates.Count == 0)
            {
                return result;
            }

            foreach (var pt in polyline.Points)
            {
                var match = candidates.FirstOrDefault(n => IsSamePoint(n.Position, pt));
                if (match != null)
                {
                    result.Add(match);
                    candidates.Remove(match);
                }
            }

            return result;
        }

        private static bool IsSamePoint(System.Drawing.PointF left, System.Drawing.PointF right)
        {
            return Math.Abs(left.X - right.X) < 0.5 && Math.Abs(left.Y - right.Y) < 0.5;
        }
    }
}
