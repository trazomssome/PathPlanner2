using DispensePath;

namespace DispenserEditor.ViewModels
{
    public sealed class GridPointViewModel : ObservableObject
    {
        private bool _isSelected;

        public int Index { get; }
        public double X { get; }
        public double Y { get; }
        public EditDispensePath.DrawToolType Tool { get; }
        public bool IsPolylineStart { get; }
        public int PolylineOrder { get; }
        public int PointIndex { get; }

        public GridPointViewModel(
            int index,
            double x,
            double y,
            EditDispensePath.DrawToolType tool,
            bool isPolylineStart,
            int polylineOrder,
            int pointIndex)
        {
            Index = index;
            X = x;
            Y = y;
            Tool = tool;
            IsPolylineStart = isPolylineStart;
            PolylineOrder = polylineOrder;
            PointIndex = pointIndex;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
