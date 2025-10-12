using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using DispensePath;
using DispenserEditor.Controls;
using DispenserEditor.Services;
using DispenserEditor.ViewModels.Base;

namespace DispenserEditor.ViewModels
{
    public class DispensePathViewModel : ObservableObject
    {
        private readonly IFileDialogService _fileDialogService;
        private readonly IMessageService _messageService;
        private IDispensePathEditor _editor;
        private DispensePolyline _editingPolyline;

        private string _recipeName = "Hello";
        private double _pixelSizeX;
        private double _pixelSizeY;
        private string _startOpenTime;
        private string _startCloseTime;
        private string _startNumOfPulse;
        private bool _canEditStartParameters;
        private DispensePointRowViewModel _selectedPoint;
        private string _moveStep = "10";
        private string _backgroundImagePath;
        private string _dx = "0";
        private string _dy = "0";
        private string _theta = "0";

        private double _alignRefX;
        private double _alignRefY;
        private double _dxValue;
        private double _dyValue;
        private double _thetaRad;

        public DispensePathViewModel(IFileDialogService fileDialogService, IMessageService messageService)
        {
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            Points = new ObservableCollection<DispensePointRowViewModel>();

            LoadRecipeCommand = new RelayCommand(LoadRecipe);
            SaveRecipeCommand = new RelayCommand(SaveRecipe);
            LoadImageCommand = new RelayCommand(LoadBackgroundImage);
            LoadTestImageCommand = new RelayCommand(LoadTestImage);
            ClearCommand = new RelayCommand(ClearRecipe);
            ZoomInCommand = new RelayCommand(() => _editor?.ZoomIn());
            ZoomOutCommand = new RelayCommand(() => _editor?.ZoomOut());
            ZoomFitCommand = new RelayCommand(() => _editor?.ZoomFit());
            PointerToolCommand = new RelayCommand(() => _editor?.SetTool(EditDispensePath.DrawToolType.Pointer));
            AddPointToolCommand = new RelayCommand(() => _editor?.SetTool(EditDispensePath.DrawToolType.AddPoint));
            PolylineToolCommand = new RelayCommand(() => _editor?.SetTool(EditDispensePath.DrawToolType.PolyLine));
            ApplyStartParametersCommand = new RelayCommand(ApplyStartParameters, () => CanEditStartParameters);
            UndoCommand = new RelayCommand(() => OperateBack(true));
            RedoCommand = new RelayCommand(() => OperateBack(false));
            AlignmentTestCommand = new RelayCommand(RunAlignmentTest);
        }

        public ObservableCollection<DispensePointRowViewModel> Points { get; }

        public string RecipeName
        {
            get => _recipeName;
            set => SetProperty(ref _recipeName, value);
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

        public string StartOpenTime
        {
            get => _startOpenTime;
            set
            {
                if (SetProperty(ref _startOpenTime, value))
                {
                    ApplyStartParametersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StartCloseTime
        {
            get => _startCloseTime;
            set
            {
                if (SetProperty(ref _startCloseTime, value))
                {
                    ApplyStartParametersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StartNumOfPulse
        {
            get => _startNumOfPulse;
            set
            {
                if (SetProperty(ref _startNumOfPulse, value))
                {
                    ApplyStartParametersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanEditStartParameters
        {
            get => _canEditStartParameters;
            private set
            {
                if (SetProperty(ref _canEditStartParameters, value))
                {
                    ApplyStartParametersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DispensePointRowViewModel SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (SetProperty(ref _selectedPoint, value))
                {
                    UpdateStartParameterEditors(value);
                }
            }
        }

        public string MoveStep
        {
            get => _moveStep;
            set => SetProperty(ref _moveStep, value);
        }

        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set => SetProperty(ref _backgroundImagePath, value);
        }

        public string Dx
        {
            get => _dx;
            set => SetProperty(ref _dx, value);
        }

        public string Dy
        {
            get => _dy;
            set => SetProperty(ref _dy, value);
        }

        public string Theta
        {
            get => _theta;
            set => SetProperty(ref _theta, value);
        }

        public RelayCommand LoadRecipeCommand { get; }
        public RelayCommand SaveRecipeCommand { get; }
        public RelayCommand LoadImageCommand { get; }
        public RelayCommand LoadTestImageCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand ZoomInCommand { get; }
        public RelayCommand ZoomOutCommand { get; }
        public RelayCommand ZoomFitCommand { get; }
        public RelayCommand PointerToolCommand { get; }
        public RelayCommand AddPointToolCommand { get; }
        public RelayCommand PolylineToolCommand { get; }
        public RelayCommand ApplyStartParametersCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand AlignmentTestCommand { get; }

        public void AttachEditor(IDispensePathEditor editor)
        {
            if (_editor != null)
            {
                _editor.PointAdded -= OnPointAdded;
                _editor.RecipeChanged -= OnRecipeChanged;
            }

            _editor = editor;

            if (_editor != null)
            {
                _editor.PointAdded += OnPointAdded;
                _editor.RecipeChanged += OnRecipeChanged;
                BackgroundImagePath = _editor.Recipe.BackgroundImagePath;
                PixelSizeX = _editor.Recipe.PixelSizeX;
                PixelSizeY = _editor.Recipe.PixelSizeY;
                RefreshFromRecipe();
            }
        }

        private void OnPointAdded(object sender, PointAddedEventArgs e)
        {
            RefreshFromRecipe();
        }

        private void OnRecipeChanged(object sender, EventArgs e)
        {
            RefreshFromRecipe();
        }

        private void RefreshFromRecipe()
        {
            SelectedPoint = null;
            Points.Clear();

            if (_editor?.Recipe == null)
            {
                return;
            }

            PixelSizeX = _editor.Recipe.PixelSizeX;
            PixelSizeY = _editor.Recipe.PixelSizeY;
            BackgroundImagePath = _editor.Recipe.BackgroundImagePath;

            int index = 1;

            if (_editor.Recipe.Nodes != null)
            {
                foreach (var node in _editor.Recipe.Nodes)
                {
                    Points.Add(new DispensePointRowViewModel(index++, node.Position.X, node.Position.Y,
                        EditDispensePath.DrawToolType.AddPoint, null, -1, false));
                }
            }

            if (_editor.Recipe.Polylines != null)
            {
                foreach (var polyline in _editor.Recipe.Polylines.OrderBy(p => p.Order))
                {
                    for (int i = 0; i < polyline.Points.Count; i++)
                    {
                        var pt = polyline.Points[i];
                        Points.Add(new DispensePointRowViewModel(index++, pt.X, pt.Y,
                            EditDispensePath.DrawToolType.PolyLine, polyline, i, i == 0));
                    }
                }
            }
        }

        private void LoadRecipe()
        {
            if (_editor == null || string.IsNullOrWhiteSpace(RecipeName))
            {
                return;
            }

            var loader = new Recipe_DispensingPoints();
            var recipe = loader.Load(RecipeName);

            _editor.SetRecipe(recipe);
            _editor.UpdateBackgroundImage();
            _editor.ZoomFit();

            RefreshFromRecipe();
        }

        private void SaveRecipe()
        {
            if (_editor?.Recipe == null || string.IsNullOrWhiteSpace(RecipeName))
            {
                return;
            }

            _editor.Recipe.PixelSizeX = PixelSizeX;
            _editor.Recipe.PixelSizeY = PixelSizeY;

            _editor.Recipe.Save(RecipeName);
            _messageService.ShowInfo("Notify", "Save Completed");
        }

        private void LoadBackgroundImage()
        {
            if (_editor == null)
            {
                return;
            }

            if (!_messageService.Confirm("Load New Image", "All data will be cleared. Do you really want to load a new image?"))
            {
                return;
            }

            var path = _fileDialogService.ShowOpenFileDialog("Image File (*.bmp)|*.bmp|All File (*.*)|*.*",
                "Select Path to load a new image");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ClearRecipeInternal();

            _editor.Recipe.BackgroundImagePath = path;
            BackgroundImagePath = path;
            _editor.UpdateBackgroundImage();
            _editor.Redraw();
        }

        private void LoadTestImage()
        {
            if (_editor == null)
            {
                return;
            }

            if (!_messageService.Confirm("Load Test Image", "All data will be remained. Do you want to load a test image?"))
            {
                return;
            }

            var path = _fileDialogService.ShowOpenFileDialog("Image File (*.bmp)|*.bmp|All File (*.*)|*.*",
                "Select Path to load a new image");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _editor.Recipe.BackgroundImagePath = path;
            BackgroundImagePath = path;
            _editor.UpdateBackgroundImage();
            _editor.Redraw();
        }

        private void ClearRecipe()
        {
            if (_editor == null)
            {
                return;
            }

            if (_messageService.Confirm("Clear", "Do you want to clear?"))
            {
                ClearRecipeInternal();
            }
        }

        private void ClearRecipeInternal()
        {
            if (_editor?.Recipe == null)
            {
                return;
            }

            _editor.Recipe.Nodes.Clear();
            _editor.Recipe.Polylines.Clear();
            _editor.Redraw();
            SelectedPoint = null;
            RefreshFromRecipe();
        }

        private void UpdateStartParameterEditors(DispensePointRowViewModel row)
        {
            if (row?.IsPolylineStart == true && row.Polyline != null)
            {
                _editingPolyline = row.Polyline;
                CanEditStartParameters = true;
                StartOpenTime = row.Polyline.OpenTimeMs.ToString("0.###");
                StartCloseTime = row.Polyline.CloseTimeMs.ToString("0.###");
                StartNumOfPulse = row.Polyline.NumOfPulse.ToString();
            }
            else
            {
                _editingPolyline = null;
                CanEditStartParameters = false;
                StartOpenTime = string.Empty;
                StartCloseTime = string.Empty;
                StartNumOfPulse = string.Empty;
            }
        }

        private void ApplyStartParameters()
        {
            if (_editingPolyline == null)
            {
                return;
            }

            if (!double.TryParse(StartOpenTime, out var openMs))
            {
                openMs = 0;
            }

            if (!double.TryParse(StartCloseTime, out var closeMs))
            {
                closeMs = 0;
            }

            if (!int.TryParse(StartNumOfPulse, out var pulse))
            {
                pulse = 0;
            }

            if (openMs < 0) openMs = 0;
            if (closeMs < 0) closeMs = 0;
            if (pulse < 0) pulse = 0;

            _editingPolyline.OpenTimeMs = openMs;
            _editingPolyline.CloseTimeMs = closeMs;
            _editingPolyline.NumOfPulse = pulse;

            _editingPolyline = null;
            CanEditStartParameters = false;
        }

        private void OperateBack(bool forward)
        {
            var editor = _editor?.UnderlyingEditor;
            if (editor == null)
            {
                return;
            }

            if (editor.CommandList.Count > 50)
            {
                editor.CommandList.RemoveAt(0);
            }

            if (editor.CommandList.Count > 0)
            {
                if (forward && editor.CtrlZ_Index < editor.CommandList.Count)
                {
                    editor.CtrlZ_Index++;
                }
                else if (!forward && editor.CtrlZ_Index > 0)
                {
                    editor.CtrlZ_Index--;
                }
                else
                {
                    return;
                }

                int idx = editor.CommandList.Count - editor.CtrlZ_Index;

                if (idx >= 0 && idx < editor.CommandList.Count)
                {
                    editor.SetList(editor.CommandList[idx]);
                    _editor.Redraw();
                    RefreshFromRecipe();
                }
            }
        }

        private void RunAlignmentTest()
        {
            if (_editor?.Recipe == null)
            {
                return;
            }

            if (!double.TryParse(Dx, out _dxValue))
            {
                _dxValue = 0;
            }

            if (!double.TryParse(Dy, out _dyValue))
            {
                _dyValue = 0;
            }

            if (!double.TryParse(Theta, out var thetaDeg))
            {
                thetaDeg = 0;
            }

            _thetaRad = thetaDeg * Math.PI / 180.0;

            double refX0 = 0;
            double refY0 = 0;
            if (_editor.Recipe.Nodes.Count > 0)
            {
                refX0 = _editor.Recipe.Nodes[0].Position.X;
                refY0 = _editor.Recipe.Nodes[0].Position.Y;
            }

            SetAlignRefOrigin(refX0, refY0);

            var alignedPolylines = new List<DispensePolyline>();
            foreach (var polyline in _editor.Recipe.Polylines)
            {
                var aligned = new DispensePolyline
                {
                    Order = polyline.Order,
                    OpenTimeMs = polyline.OpenTimeMs,
                    CloseTimeMs = polyline.CloseTimeMs,
                    NumOfPulse = polyline.NumOfPulse,
                    Stroke = polyline.Stroke,
                    StrokeWidth = polyline.StrokeWidth,
                    Use = polyline.Use
                };

                foreach (var point in polyline.Points)
                {
                    var (cvtX, cvtY) = CompensatePathPoint(point.X, point.Y);
                    aligned.Points.Add(new PointF((float)cvtX, (float)cvtY));
                }

                alignedPolylines.Add(aligned);
            }

            ClearRecipeInternal();

            _editor.Recipe.Nodes.Add(new DispensePointNode(new PointF((float)(_alignRefX + _dxValue), (float)(_alignRefY + _dyValue))));
            foreach (var polyline in alignedPolylines)
            {
                _editor.Recipe.Polylines.Add(polyline);
            }

            _editor.UpdateBackgroundImage();
            _editor.Redraw();
            RefreshFromRecipe();
        }

        private void SetAlignRefOrigin(double refX0, double refY0)
        {
            _alignRefX = refX0;
            _alignRefY = refY0;
        }

        private (double X, double Y) CompensatePathPoint(double x, double y)
        {
            double cos = Math.Cos(_thetaRad);
            double sin = Math.Sin(_thetaRad);

            double cvtX = cos * (x - _alignRefX) - sin * (y - _alignRefY) + _alignRefX + _dxValue;
            double cvtY = sin * (x - _alignRefX) + cos * (y - _alignRefY) + _alignRefY + _dyValue;
            return (cvtX, cvtY);
        }
    }
}
