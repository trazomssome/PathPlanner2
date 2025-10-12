using DispensePath;
using DispenserEditor.ViewModels.Base;

namespace DispenserEditor.ViewModels
{
    public class DispensePointRowViewModel : ObservableObject
    {
        public DispensePointRowViewModel(int index, double x, double y, EditDispensePath.DrawToolType tool,
            DispensePolyline polyline, int pointIndex, bool isPolylineStart)
        {
            Index = index;
            X = x;
            Y = y;
            Tool = tool;
            Polyline = polyline;
            PointIndex = pointIndex;
            IsPolylineStart = isPolylineStart;
        }

        public int Index { get; }
        public double X { get; }
        public double Y { get; }
        public EditDispensePath.DrawToolType Tool { get; }
        public string ToolName => Tool.ToString();
        public bool IsPolylineStart { get; }
        public DispensePolyline Polyline { get; }
        public int PointIndex { get; }
    }
}
