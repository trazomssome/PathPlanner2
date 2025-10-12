
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using DispenserEditor.Models;
using DispenserEditor.Services;
using Microsoft.Win32;

namespace DispenserEditor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly UndoRedoService _history = new UndoRedoService();
        private IntegratedRecipe _integratedRecipe = new IntegratedRecipe();
        private bool _isRestoring;
        private bool _interactionInProgress;
        private PathCoordinateViewModel _selectedCoordinate;
        private EditorMode _mode = EditorMode.Move;
        private double _mmPerPixel = 0.1;
        private double _canvasWidth = 800;
        private double _canvasHeight = 600;
        private double _centerPixelX = 400;
        private double _centerPixelY = 300;
        private double _defaultSpeed = 100;
        private bool _speedLimitEnabled;
        private double _speedLimit = 500;
        private double _recipeOpacity = 0.85;
        private BitmapImage _referenceImage;
        private string _referenceImagePath;
        private int _currentLineGroup = 1;

        public MainViewModel()
        {
            Coordinates.CollectionChanged += OnCoordinatesChanged;
            InitializeRecipe();

            UndoCommand = new RelayCommand(_ => Undo(), _ => _history.CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => _history.CanRedo);
            DeleteCoordinateCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedCoordinate != null);
            MoveUpCommand = new RelayCommand(_ => MoveSelected(-1), _ => CanMove(-1));
            MoveDownCommand = new RelayCommand(_ => MoveSelected(1), _ => CanMove(1));
            ExportCommand = new RelayCommand(_ => ExportRecipe());
            LoadImageCommand = new RelayCommand(_ => LoadImage());
        }

        public ObservableCollection<PathCoordinateViewModel> Coordinates { get; } = new ObservableCollection<PathCoordinateViewModel>();
        public ObservableCollection<LineSegmentViewModel> LineSegments { get; } = new ObservableCollection<LineSegmentViewModel>();

        public PathCoordinateViewModel SelectedCoordinate
        {
            get => _selectedCoordinate;
            set
            {
                if (SetProperty(ref _selectedCoordinate, value))
                {
                    DeleteCoordinateCommand.RaiseCanExecuteChanged();
                    MoveUpCommand.RaiseCanExecuteChanged();
                    MoveDownCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public EditorMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value)
                {
                    return;
                }

                var previous = _mode;
                if (SetProperty(ref _mode, value))
                {
                    if (previous == EditorMode.Line && value != EditorMode.Line)
                    {
                        _currentLineGroup++;
                    }
                }
            }
        }

        public double MmPerPixel
        {
            get => _mmPerPixel;
            set
            {
                if (value <= 0)
                {
                    return;
                }

                if (SetProperty(ref _mmPerPixel, value))
                {
                    UpdatePixelPositions();
                    CaptureHistory();
                }
            }
        }

        public double CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                if (SetProperty(ref _canvasWidth, value))
                {
                    UpdateCenterDefaults();
                    UpdatePixelPositions();
                }
            }
        }

        public double CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                if (SetProperty(ref _canvasHeight, value))
                {
                    UpdateCenterDefaults();
                    UpdatePixelPositions();
                }
            }
        }

        public double CenterPixelX
        {
            get => _centerPixelX;
            private set
            {
                if (SetProperty(ref _centerPixelX, value))
                {
                    UpdatePixelPositions();
                }
            }
        }

        public double CenterPixelY
        {
            get => _centerPixelY;
            private set
            {
                if (SetProperty(ref _centerPixelY, value))
                {
                    UpdatePixelPositions();
                }
            }
        }

        public double DefaultSpeed
        {
            get => _defaultSpeed;
            set
            {
                if (SetProperty(ref _defaultSpeed, value))
                {
                    CaptureHistory();
                }
            }
        }

        public bool SpeedLimitEnabled
        {
            get => _speedLimitEnabled;
            set
            {
                if (SetProperty(ref _speedLimitEnabled, value))
                {
                    CaptureHistory();
                }
            }
        }

        public double SpeedLimit
        {
            get => _speedLimit;
            set
            {
                if (SetProperty(ref _speedLimit, value))
                {
                    CaptureHistory();
                }
            }
        }

        public double RecipeOpacity
        {
            get => _recipeOpacity;
            set
            {
                if (SetProperty(ref _recipeOpacity, value))
                {
                    CaptureHistory();
                }
            }
        }

        public BitmapImage ReferenceImage
        {
            get => _referenceImage;
            private set
            {
                if (SetProperty(ref _referenceImage, value))
                {
                    UpdateImageMetrics();
                }
            }
        }

        public string ReferenceImagePath
        {
            get => _referenceImagePath;
            private set => SetProperty(ref _referenceImagePath, value);
        }

        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand DeleteCoordinateCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand LoadImageCommand { get; }

        public void AddCoordinateFromPixel(CoordinateType type, Point pixelPosition)
        {
            var coordinate = new PathCoordinateViewModel(OnCoordinateChanged)
            {
                Type = type,
                Name = GenerateName(type),
                DispenseEnabled = true,
                UseAutoSmoothing = true
            };

            coordinate.UpdateFromPixel(pixelPosition.X, pixelPosition.Y, MmPerPixel, CenterPixelX, CenterPixelY);

            if (type == CoordinateType.Line)
            {
                coordinate.LineGroup = _currentLineGroup;
            }

            CaptureHistory();
            Coordinates.Add(coordinate);
            SelectedCoordinate = coordinate;
            UpdateLineSegments();
        }

        public void CompleteLine()
        {
            _currentLineGroup++;
        }

        public void SetCenterFromPixel(Point pixelPosition)
        {
            var previousCenter = new Point(CenterPixelX, CenterPixelY);
            var newCenter = pixelPosition;

            if (Math.Abs(previousCenter.X - newCenter.X) < 0.001 && Math.Abs(previousCenter.Y - newCenter.Y) < 0.001)
            {
                return;
            }

            BeginInteractiveChange();
            foreach (var coordinate in Coordinates)
            {
                var currentPixel = new Point(coordinate.PixelX, coordinate.PixelY);
                coordinate.UpdateFromPixel(currentPixel.X, currentPixel.Y, MmPerPixel, newCenter.X, newCenter.Y);
            }

            CenterPixelX = newCenter.X;
            CenterPixelY = newCenter.Y;
            UpdateLineSegments();
            EndInteractiveChange();
        }

        public void UpdateCenterToImageMiddle()
        {
            BeginInteractiveChange();
            CenterPixelX = CanvasWidth / 2.0;
            CenterPixelY = CanvasHeight / 2.0;
            UpdatePixelPositions();
            UpdateLineSegments();
            EndInteractiveChange();
        }

        public void UpdateCoordinateFromPixel(PathCoordinateViewModel coordinate, Point pixelPosition)
        {
            coordinate.UpdateFromPixel(pixelPosition.X, pixelPosition.Y, MmPerPixel, CenterPixelX, CenterPixelY);
            UpdateLineSegments();
        }

        public void BeginInteractiveChange()
        {
            if (_isRestoring || _interactionInProgress)
            {
                return;
            }

            _interactionInProgress = true;
            _history.Record(BuildModel());
            RaiseHistoryChanged();
        }

        public void EndInteractiveChange()
        {
            if (_isRestoring || !_interactionInProgress)
            {
                return;
            }

            _interactionInProgress = false;
            _history.Record(BuildModel());
            RaiseHistoryChanged();
        }

        public void ValidateRecipe()
        {
            if (SpeedLimitEnabled)
            {
                foreach (var coordinate in Coordinates)
                {
                    var speed = coordinate.UseCustomSpeed ? coordinate.Speed : DefaultSpeed;
                    if (speed > SpeedLimit)
                    {
                        throw new InvalidOperationException($"{coordinate.Name} 속도가 제한을 초과했습니다.");
                    }
                }
            }
        }

        public IntegratedRecipe BuildModel()
        {
            var recipe = new DispenseRecipe
            {
                Name = _integratedRecipe.Recipes.First().Name,
                DefaultSpeed = DefaultSpeed,
                SpeedLimitEnabled = SpeedLimitEnabled,
                SpeedLimit = SpeedLimit,
                Opacity = RecipeOpacity,
                Coordinates = new ObservableCollection<PathCoordinate>(Coordinates.Select(c => c.ToModel()))
            };

            return new IntegratedRecipe
            {
                SchemaVersion = _integratedRecipe.SchemaVersion,
                Recipes = new ObservableCollection<DispenseRecipe> { recipe }
            };
        }

        public void ApplyModel(IntegratedRecipe model, bool resetHistory = true)
        {
            _isRestoring = true;
            try
            {
                _integratedRecipe = model;
                Coordinates.Clear();

                var recipe = _integratedRecipe.Recipes.FirstOrDefault() ?? new DispenseRecipe();
                if (!_integratedRecipe.Recipes.Any())
                {
                    _integratedRecipe.Recipes.Add(recipe);
                }

                DefaultSpeed = recipe.DefaultSpeed;
                SpeedLimitEnabled = recipe.SpeedLimitEnabled;
                SpeedLimit = recipe.SpeedLimit;
                RecipeOpacity = recipe.Opacity;

                foreach (var coordinate in recipe.Coordinates)
                {
                    var vm = PathCoordinateViewModel.FromModel(coordinate, OnCoordinateChanged);
                    Coordinates.Add(vm);
                }

                UpdatePixelPositions();
                UpdateLineSegments();
                _currentLineGroup = (Coordinates.Any() ? Coordinates.Max(c => c.LineGroup) : 0) + 1;
                if (resetHistory)
                {
                    _history.Reset(BuildModel());
                }
                RaiseHistoryChanged();
            }
            finally
            {
                _isRestoring = false;
            }
        }

        private void InitializeRecipe()
        {
            _integratedRecipe = new IntegratedRecipe();
            var recipe = new DispenseRecipe();
            _integratedRecipe.Recipes.Add(recipe);
            ApplyModel(_integratedRecipe);
        }

        private void OnCoordinateChanged(PathCoordinateViewModel coordinate, string propertyName)
        {
            if (_isRestoring)
            {
                return;
            }

            coordinate.UpdatePixelPosition(MmPerPixel, CenterPixelX, CenterPixelY);
            UpdateLineSegments();
            if (!_interactionInProgress)
            {
                CaptureHistory();
            }
        }

        private void OnCoordinatesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isRestoring)
            {
                return;
            }

            UpdateLineSegments();
            if (!_interactionInProgress)
            {
                CaptureHistory();
            }
        }

        private void CaptureHistory()
        {
            if (_isRestoring)
            {
                return;
            }

            _history.Record(BuildModel());
            RaiseHistoryChanged();
        }

        private void RaiseHistoryChanged()
        {
            UndoCommand?.RaiseCanExecuteChanged();
            RedoCommand?.RaiseCanExecuteChanged();
        }

        private void UpdatePixelPositions()
        {
            foreach (var coordinate in Coordinates)
            {
                coordinate.UpdatePixelPosition(MmPerPixel, CenterPixelX, CenterPixelY);
            }

            UpdateLineSegments();
        }

        private void UpdateLineSegments()
        {
            LineSegments.Clear();
            PathCoordinateViewModel previous = null;
            foreach (var coordinate in Coordinates)
            {
                if (coordinate.Type == CoordinateType.Line && previous?.Type == CoordinateType.Line && previous.LineGroup == coordinate.LineGroup)
                {
                    LineSegments.Add(new LineSegmentViewModel
                    {
                        X1 = previous.PixelX,
                        Y1 = previous.PixelY,
                        X2 = coordinate.PixelX,
                        Y2 = coordinate.PixelY
                    });
                }

                previous = coordinate.Type == CoordinateType.Line ? coordinate : null;
            }
        }

        private void UpdateCenterDefaults()
        {
            if (double.IsNaN(CanvasWidth) || double.IsNaN(CanvasHeight) || CanvasWidth <= 0 || CanvasHeight <= 0)
            {
                return;
            }

            if (Math.Abs(CenterPixelX) < 0.0001 && Math.Abs(CenterPixelY) < 0.0001)
            {
                CenterPixelX = CanvasWidth / 2.0;
                CenterPixelY = CanvasHeight / 2.0;
            }
        }

        private void UpdateImageMetrics()
        {
            if (ReferenceImage != null)
            {
                CanvasWidth = ReferenceImage.PixelWidth;
                CanvasHeight = ReferenceImage.PixelHeight;
                CenterPixelX = CanvasWidth / 2.0;
                CenterPixelY = CanvasHeight / 2.0;
            }
        }

        private string GenerateName(CoordinateType type)
        {
            var prefix = type == CoordinateType.Line ? "Line" : "Point";
            var index = Coordinates.Count(c => c.Type == type) + 1;
            return $"{prefix}-{index:000}";
        }

        private bool CanMove(int direction)
        {
            if (SelectedCoordinate == null)
            {
                return false;
            }

            var index = Coordinates.IndexOf(SelectedCoordinate);
            if (index < 0)
            {
                return false;
            }

            var target = index + direction;
            return target >= 0 && target < Coordinates.Count;
        }

        private void MoveSelected(int direction)
        {
            if (!CanMove(direction) || SelectedCoordinate == null)
            {
                return;
            }

            CaptureHistory();
            var index = Coordinates.IndexOf(SelectedCoordinate);
            var target = index + direction;
            Coordinates.Move(index, target);
            UpdateLineSegments();
        }

        private void DeleteSelected()
        {
            if (SelectedCoordinate == null)
            {
                return;
            }

            CaptureHistory();
            Coordinates.Remove(SelectedCoordinate);
            SelectedCoordinate = null;
            UpdateLineSegments();
        }

        private void Undo()
        {
            var state = _history.Undo();
            if (state == null)
            {
                return;
            }

            ApplyModel(state, resetHistory: false);
        }

        private void Redo()
        {
            var state = _history.Redo();
            if (state == null)
            {
                return;
            }

            ApplyModel(state, resetHistory: false);
        }

        private void ExportRecipe()
        {
            try
            {
                ValidateRecipe();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "검증 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                FileName = "dispense_recipe.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var model = BuildModel();
                RecipePersistenceService.Save(dialog.FileName, model);
            }
        }

        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(dialog.FileName);
                bitmap.EndInit();
                bitmap.Freeze();

                ReferenceImagePath = dialog.FileName;
                ReferenceImage = bitmap;
            }
        }
    }
}

