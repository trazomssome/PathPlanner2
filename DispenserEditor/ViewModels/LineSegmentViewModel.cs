namespace DispenserEditor.ViewModels
{

    public class LineSegmentViewModel : ObservableObject
    {
        private double _x1;
        private double _y1;
        private double _x2;
        private double _y2;

        public double X1
        {
            get => _x1;
            set => SetProperty(ref _x1, value);
        }

        public double Y1
        {
            get => _y1;
            set => SetProperty(ref _y1, value);
        }

        public double X2
        {
            get => _x2;
            set => SetProperty(ref _x2, value);
        }

        public double Y2
        {
            get => _y2;
            set => SetProperty(ref _y2, value);
        }
    }
}


