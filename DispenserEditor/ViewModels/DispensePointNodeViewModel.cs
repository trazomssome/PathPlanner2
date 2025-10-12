using System.Drawing;
using DispensePath;

namespace DispenserEditor.ViewModels
{
    public sealed class DispensePointNodeViewModel : ObservableObject
    {
        private const double MarkerRadius = 5.0;

        private double _x;
        private double _y;
        private double _offsetZ;
        private double _speed;
        private bool _use = true;
        private bool _isSelected;

        private double _canvasLeft;
        private double _canvasTop;

        public DispensePointNodeViewModel()
        {
            UpdateCanvasPosition();
        }

        public DispensePointNodeViewModel(DispensePointNode node)
        {
            if (node != null)
            {
                _x = node.Position.X;
                _y = node.Position.Y;
                _offsetZ = node.OffsetZ;
                _speed = node.Speed;
                _use = node.Use;
            }

            UpdateCanvasPosition();
        }

        public double X
        {
            get => _x;
            set
            {
                if (SetProperty(ref _x, value))
                {
                    UpdateCanvasPosition();
                }
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (SetProperty(ref _y, value))
                {
                    UpdateCanvasPosition();
                }
            }
        }

        public double OffsetZ
        {
            get => _offsetZ;
            set => SetProperty(ref _offsetZ, value);
        }

        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public bool Use
        {
            get => _use;
            set => SetProperty(ref _use, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public double CanvasLeft
        {
            get => _canvasLeft;
            private set => SetProperty(ref _canvasLeft, value);
        }

        public double CanvasTop
        {
            get => _canvasTop;
            private set => SetProperty(ref _canvasTop, value);
        }

        public DispensePointNode ToModel()
        {
            var node = new DispensePointNode
            {
                Position = new PointF((float)X, (float)Y),
                OffsetZ = OffsetZ,
                Speed = Speed,
                Use = Use
            };

            return node;
        }

        private void UpdateCanvasPosition()
        {
            CanvasLeft = _x - MarkerRadius;
            CanvasTop = _y - MarkerRadius;
        }
    }
}
