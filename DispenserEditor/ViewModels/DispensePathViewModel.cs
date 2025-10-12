using DispensePath;
using DispenserEditor.Commands;
using DispenserEditor.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace DispenserEditor.ViewModels
{
    public sealed class DispensePathViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly EditDispensePath _graphicEditor;
        private readonly PolylineParameterViewModel _startParameters = new PolylineParameterViewModel();
        private GridPointViewModel _selectedPoint;
        private string _recipeName = "Hello";
        private string _backgroundImagePath = string.Empty;
        private double _pixelSizeX = 0.01;
        private double _pixelSizeY = 0.01;
        private string _alignDx = "0";
        private string _alignDy = "0";
        private string _alignTheta = "0";
        private string _unitX = "0";
        private string _unitY = "0";
        private string _camToDispX = "30";
        private string _camToDispY = "0";
        private GridPointViewModel _editingStartPoint;

        public DispensePathViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _graphicEditor = new EditDispensePath("DispenseEditorWpf");
            _graphicEditor.PointAdded += (s, e) => RefreshGraphicGrid();

            Points = new ObservableCollection<GridPointViewModel>();

            LoadImageCommand = new RelayCommand(LoadImage);
            LoadTestImageCommand = new RelayCommand(LoadTestImage);
            SaveParametersCommand = new RelayCommand(SaveParameters);
            LoadParametersCommand = new RelayCommand(LoadParameters);
            ClearCommand = new RelayCommand(RequestClear);
            ZoomInCommand = new RelayCommand(() => _graphicEditor.ZoomIn());
            ZoomOutCommand = new RelayCommand(() => _graphicEditor.ZoomOut());
            ZoomFitCommand = new RelayCommand(() => _graphicEditor.ZoomFit());
            PointerToolCommand = new RelayCommand(() => _graphicEditor.ActiveTool = EditDispensePath.DrawToolType.Pointer);
            AddPointToolCommand = new RelayCommand(() => _graphicEditor.ActiveTool = EditDispensePath.DrawToolType.AddPoint);
            ApplyStartParametersCommand = new RelayCommand(ApplyStartParameters);
            UndoCommand = new RelayCommand(() => OperateBack(true));
            RedoCommand = new RelayCommand(() => OperateBack(false));
            AddCommand = new RelayCommand(() => _graphicEditor.Invalidate());
            DeleteCommand = new RelayCommand(() => _graphicEditor.Invalidate());
            TestAlignCommand = new RelayCommand(ApplyAlignCompensation);

            _startParameters.Clear();
        }

        public ObservableCollection<GridPointViewModel> Points { get; }

        public PolylineParameterViewModel StartParameters => _startParameters;

        public string RecipeName
        {
            get => _recipeName;
            set => SetProperty(ref _recipeName, value);
        }

        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set => SetProperty(ref _backgroundImagePath, value);
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

        public string AlignDx
        {
            get => _alignDx;
            set => SetProperty(ref _alignDx, value);
        }

        public string AlignDy
        {
            get => _alignDy;
            set => SetProperty(ref _alignDy, value);
        }

        public string AlignTheta
        {
            get => _alignTheta;
            set => SetProperty(ref _alignTheta, value);
        }

        public string UnitX
        {
            get => _unitX;
            set => SetProperty(ref _unitX, value);
        }

        public string UnitY
        {
            get => _unitY;
            set => SetProperty(ref _unitY, value);
        }

        public string CamToDispX
        {
            get => _camToDispX;
            set => SetProperty(ref _camToDispX, value);
        }

        public string CamToDispY
        {
            get => _camToDispY;
            set => SetProperty(ref _camToDispY, value);
        }

        public GridPointViewModel SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (SetProperty(ref _selectedPoint, value))
                {
                    UpdateSelection(value);
                }
            }
        }

        public RelayCommand LoadImageCommand { get; }
        public RelayCommand LoadTestImageCommand { get; }
        public RelayCommand SaveParametersCommand { get; }
        public RelayCommand LoadParametersCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand ZoomInCommand { get; }
        public RelayCommand ZoomOutCommand { get; }
        public RelayCommand ZoomFitCommand { get; }
        public RelayCommand PointerToolCommand { get; }
        public RelayCommand AddPointToolCommand { get; }
        public RelayCommand ApplyStartParametersCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand TestAlignCommand { get; }

        public EditDispensePath GraphicEditor => _graphicEditor;

        public void RefreshGraphicGrid()
        {
            Points.Clear();

            int no = 1;

            var nodes = _graphicEditor?.Recipe_DispPoints?.Nodes;
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    Points.Add(new GridPointViewModel(
                        no++,
                        node.Position.X,
                        node.Position.Y,
                        EditDispensePath.DrawToolType.AddPoint,
                        false,
                        -1,
                        -1));
                }
            }

            var polylines = _graphicEditor?.Recipe_DispPoints?.Polylines;
            if (polylines != null)
            {
                foreach (var pl in polylines)
                {
                    for (int j = 0; j < pl.Points.Count; j++)
                    {
                        var p = pl.Points[j];
                        Points.Add(new GridPointViewModel(
                            no++,
                            p.X,
                            p.Y,
                            EditDispensePath.DrawToolType.PolyLine,
                            j == 0,
                            pl.Order,
                            j));
                    }
                }
            }
        }

        private void UpdateSelection(GridPointViewModel point)
        {
            _startParameters.IsEnabled = false;
            _editingStartPoint = null;

            if (point == null)
            {
                _startParameters.Clear();
                return;
            }

            if (point.Tool == EditDispensePath.DrawToolType.PolyLine && point.IsPolylineStart)
            {
                var pl = _graphicEditor?.Recipe_DispPoints?.Polylines?
                    .FirstOrDefault(x => x.Order == point.PolylineOrder);
                if (pl != null)
                {
                    _editingStartPoint = point;
                    _startParameters.OpenTime = pl.OpenTimeMs;
                    _startParameters.CloseTime = pl.CloseTimeMs;
                    _startParameters.Pulse = pl.NumOfPulse;
                    _startParameters.IsEnabled = true;
                    return;
                }
            }

            _startParameters.Clear();
        }

        private void ApplyStartParameters()
        {
            if (_editingStartPoint == null)
            {
                return;
            }

            var polyline = _graphicEditor?.Recipe_DispPoints?.Polylines?
                .FirstOrDefault(x => x.Order == _editingStartPoint.PolylineOrder);
            if (polyline == null)
            {
                return;
            }

            polyline.OpenTimeMs = _startParameters.OpenTime;
            polyline.CloseTimeMs = _startParameters.CloseTime;
            polyline.NumOfPulse = _startParameters.Pulse;
        }

        private void LoadImage()
        {
            if (!_dialogService.ShowConfirmation("Load New Image", "All data will be cleared. Do you really want to load a new image?"))
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Image File (*.bmp)|*.bmp|All File (*.*)|*.*",
                Title = "Select Path to load a new image"
            };

            if (dialog.ShowDialog() == true)
            {
                ClearAll();
                _graphicEditor.Recipe_DispPoints.BackgroundImagePath = dialog.FileName;
                BackgroundImagePath = dialog.FileName;
                _graphicEditor.UpdateBackgroundImage();
                _graphicEditor.Invalidate();
                _graphicEditor.ZoomFit();
            }
        }

        private void LoadTestImage()
        {
            if (!_dialogService.ShowConfirmation("Load Test Image", "All data will be remained. Do you want to load a test image?"))
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Image File (*.bmp)|*.bmp|All File (*.*)|*.*",
                Title = "Select Path to load a new image"
            };

            if (dialog.ShowDialog() == true)
            {
                _graphicEditor.Recipe_DispPoints.BackgroundImagePath = dialog.FileName;
                BackgroundImagePath = dialog.FileName;
                _graphicEditor.UpdateBackgroundImage();
                _graphicEditor.Invalidate();
                _graphicEditor.ZoomFit();
            }
        }

        private void SaveParameters()
        {
            _graphicEditor.Recipe_DispPoints.PixelSizeX = _pixelSizeX;
            _graphicEditor.Recipe_DispPoints.PixelSizeY = _pixelSizeY;
            _graphicEditor.Recipe_DispPoints.Save(_recipeName);
            _dialogService.ShowInformation("Notify", "Save Completed");
        }

        private void LoadParameters()
        {
            _graphicEditor.Recipe_DispPoints = _graphicEditor.Recipe_DispPoints.Load(_recipeName);
            BackgroundImagePath = _graphicEditor.Recipe_DispPoints.BackgroundImagePath;
            PixelSizeX = _graphicEditor.Recipe_DispPoints.PixelSizeX;
            PixelSizeY = _graphicEditor.Recipe_DispPoints.PixelSizeY;
            _graphicEditor.UpdateBackgroundImage();
            _graphicEditor.Invalidate();
            RefreshGraphicGrid();
            _graphicEditor.ZoomFit();
            SelectedPoint = null;
        }

        private void ClearAll()
        {
            if (_graphicEditor?.Recipe_DispPoints != null)
            {
                _graphicEditor.Recipe_DispPoints.Nodes.Clear();
                _graphicEditor.Recipe_DispPoints.Polylines.Clear();
                _graphicEditor.RedrawComposite();
            }

            Points.Clear();
            _startParameters.Clear();
            _editingStartPoint = null;
            SelectedPoint = null;
        }

        private void RequestClear()
        {
            if (_dialogService.ShowConfirmation("Clear", "Do you want to clear?"))
            {
                ClearAll();
            }
        }

        private void OperateBack(bool backOrForward)
        {
            if (_graphicEditor.CommandList.Count <= 0)
            {
                return;
            }

            if (backOrForward)
            {
                if (_graphicEditor.CtrlZ_Index < _graphicEditor.CommandList.Count - 1)
                {
                    _graphicEditor.CtrlZ_Index++;
                }
            }
            else
            {
                if (_graphicEditor.CtrlZ_Index > 0)
                {
                    _graphicEditor.CtrlZ_Index--;
                }
            }

            int idx = _graphicEditor.CommandList.Count - _graphicEditor.CtrlZ_Index;
            if (idx >= 0 && idx < _graphicEditor.CommandList.Count)
            {
                _graphicEditor.SetList(_graphicEditor.CommandList[idx]);
            }
        }

        private void ApplyAlignCompensation()
        {
            if (!double.TryParse(_alignDx, out var dx)) dx = 0;
            if (!double.TryParse(_alignDy, out var dy)) dy = 0;
            if (!double.TryParse(_alignTheta, out var thetaDeg)) thetaDeg = 0;
            _ = double.TryParse(_unitX, out _);
            _ = double.TryParse(_unitY, out _);
            _ = double.TryParse(_camToDispX, out _);
            _ = double.TryParse(_camToDispY, out _);

            double theta = thetaDeg * Math.PI / 180.0;

            var points = _graphicEditor.Recipe_DispPoints;
            if (points == null)
            {
                return;
            }

            if (points.Nodes.Count == 0)
            {
                return;
            }

            double refX0 = points.Nodes[0].Position.X;
            double refY0 = points.Nodes[0].Position.Y;

            SetAlignRefOrg(refX0, refY0);

            var polylines = new System.Collections.Generic.List<DispensePolyline>();
            foreach (var polyline in points.Polylines)
            {
                var polylineAlign = new DispensePolyline
                {
                    Order = polyline.Order,
                    OpenTimeMs = polyline.OpenTimeMs,
                    CloseTimeMs = polyline.CloseTimeMs,
                    NumOfPulse = polyline.NumOfPulse,
                    Use = polyline.Use
                };

                foreach (var point in polyline.Points)
                {
                    var compensated = CompensatePathPoint(point.X, point.Y, dx, dy, theta);
                    polylineAlign.Points.Add(new System.Drawing.PointF((float)compensated.X, (float)compensated.Y));
                }

                polylines.Add(polylineAlign);
            }

            ClearAll();

            points.Nodes.Add(new DispensePointNode(new PointF((float)(_alignRefX0 + dx), (float)(_alignRefY0 + dy))));
            foreach (var polylineAlign in polylines)
            {
                points.Polylines.Add(polylineAlign);
            }

            _graphicEditor.UpdateBackgroundImage();
            _graphicEditor.Invalidate();
            RefreshGraphicGrid();
        }

        private double _alignRefX0;
        private double _alignRefY0;

        private void SetAlignRefOrg(double refX0, double refY0)
        {
            _alignRefX0 = refX0;
            _alignRefY0 = refY0;
        }

        private (double X, double Y) CompensatePathPoint(double x, double y, double dx, double dy, double theta)
        {
            double cvtX = Math.Cos(theta) * (x - _alignRefX0) - Math.Sin(theta) * (y - _alignRefY0) + _alignRefX0 + dx;
            double cvtY = Math.Sin(theta) * (x - _alignRefX0) + Math.Cos(theta) * (y - _alignRefY0) + _alignRefY0 + dy;
            return (cvtX, cvtY);
        }
    }
}
