using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DispensePath;

namespace DispenserEditor.ViewModels
{
    public sealed class DispensePolylineViewModel : ObservableObject
    {
        private readonly ObservableCollection<DispensePointNodeViewModel> _points;

        private int _order;
        private bool _use = true;
        private double _openTimeMs;
        private double _closeTimeMs;
        private int _numOfPulse;
        private double _strokeWidth = 2.0;
        private Color _strokeColor = Colors.DeepSkyBlue;
        private bool _isSelected;
        private PointCollection _geometryPoints = new PointCollection();

        public DispensePolylineViewModel()
        {
            _points = new ObservableCollection<DispensePointNodeViewModel>();
            _points.CollectionChanged += PointsOnCollectionChanged;
        }

        public DispensePolylineViewModel(DispensePolyline polyline)
            : this(polyline, null)
        {
        }

        public DispensePolylineViewModel(DispensePolyline polyline, System.Collections.Generic.IEnumerable<DispensePointNode> nodes)
            : this()
        {
            if (polyline == null)
            {
                return;
            }

            _order = polyline.Order;
            _use = polyline.Use;
            _openTimeMs = polyline.OpenTimeMs;
            _closeTimeMs = polyline.CloseTimeMs;
            _numOfPulse = polyline.NumOfPulse;
            _strokeWidth = polyline.StrokeWidth;
            _strokeColor = ConvertColor(polyline.Stroke);

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null)
                    {
                        _points.Add(new DispensePointNodeViewModel(node));
                    }
                }
            }
            else
            {
                foreach (var pt in polyline.Points)
                {
                    AddPoint(pt.X, pt.Y);
                }
            }

            UpdateGeometry();
        }

        public ObservableCollection<DispensePointNodeViewModel> Points => _points;

        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public bool Use
        {
            get => _use;
            set => SetProperty(ref _use, value);
        }

        public double OpenTimeMs
        {
            get => _openTimeMs;
            set => SetProperty(ref _openTimeMs, value);
        }

        public double CloseTimeMs
        {
            get => _closeTimeMs;
            set => SetProperty(ref _closeTimeMs, value);
        }

        public int NumOfPulse
        {
            get => _numOfPulse;
            set => SetProperty(ref _numOfPulse, value);
        }

        public double StrokeWidth
        {
            get => _strokeWidth;
            set
            {
                if (SetProperty(ref _strokeWidth, value))
                {
                    OnPropertyChanged(nameof(StrokeThickness));
                }
            }
        }

        public double StrokeThickness => _strokeWidth;

        public Color StrokeColor
        {
            get => _strokeColor;
            set
            {
                if (SetProperty(ref _strokeColor, value))
                {
                    OnPropertyChanged(nameof(StrokeBrush));
                }
            }
        }

        public Brush StrokeBrush => new SolidColorBrush(_strokeColor);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public PointCollection GeometryPoints
        {
            get => _geometryPoints;
            private set => SetProperty(ref _geometryPoints, value);
        }

        public DispensePointNodeViewModel AddPoint(double x, double y)
        {
            var node = new DispensePointNodeViewModel
            {
                X = x,
                Y = y
            };

            _points.Add(node);
            return node;
        }

        public DispensePointNodeViewModel AddPoint(DispensePointNode node)
        {
            if (node == null)
            {
                return null;
            }

            var vm = new DispensePointNodeViewModel(node);
            _points.Add(vm);
            return vm;
        }

        public DispensePointNodeViewModel InsertPoint(int index, double x, double y)
        {
            index = Math.Max(0, Math.Min(index, _points.Count));
            var node = new DispensePointNodeViewModel
            {
                X = x,
                Y = y
            };

            _points.Insert(index, node);
            return node;
        }

        public void RemovePoint(DispensePointNodeViewModel node)
        {
            if (node == null)
            {
                return;
            }

            node.PropertyChanged -= NodeOnPropertyChanged;
            _points.Remove(node);
        }

        public void ClearPoints()
        {
            foreach (var node in _points)
            {
                node.PropertyChanged -= NodeOnPropertyChanged;
            }

            _points.Clear();
        }

        public void FlipHorizontal()
        {
            if (_points.Count == 0)
            {
                return;
            }

            var minX = _points.Min(p => p.X);
            var maxX = _points.Max(p => p.X);
            var center = (minX + maxX) / 2.0;

            foreach (var node in _points)
            {
                node.X = center * 2 - node.X;
            }

            UpdateGeometry();
        }

        public void FlipVertical()
        {
            if (_points.Count == 0)
            {
                return;
            }

            var minY = _points.Min(p => p.Y);
            var maxY = _points.Max(p => p.Y);
            var center = (minY + maxY) / 2.0;

            foreach (var node in _points)
            {
                node.Y = center * 2 - node.Y;
            }

            UpdateGeometry();
        }

        public DispensePolyline ToModel()
        {
            var polyline = new DispensePolyline
            {
                Order = Order,
                Use = Use,
                OpenTimeMs = OpenTimeMs,
                CloseTimeMs = CloseTimeMs,
                NumOfPulse = NumOfPulse,
                StrokeWidth = (float)StrokeWidth,
                Stroke = System.Drawing.Color.FromArgb(StrokeColor.A, StrokeColor.R, StrokeColor.G, StrokeColor.B),
                IsSelected = IsSelected
            };

            foreach (var node in _points)
            {
                polyline.Points.Add(new System.Drawing.PointF((float)node.X, (float)node.Y));
            }

            return polyline;
        }

        private static Color ConvertColor(System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void PointsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DispensePointNodeViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= NodeOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (DispensePointNodeViewModel item in e.NewItems)
                {
                    item.PropertyChanged += NodeOnPropertyChanged;
                }
            }

            UpdateGeometry();
        }

        private void NodeOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DispensePointNodeViewModel.X) ||
                e.PropertyName == nameof(DispensePointNodeViewModel.Y))
            {
                UpdateGeometry();
            }
        }

        private void UpdateGeometry()
        {
            var pc = new PointCollection(_points.Select(p => new Point(p.X, p.Y)));
            GeometryPoints = pc;
        }
    }
}
