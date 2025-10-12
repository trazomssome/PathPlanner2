namespace DispenserEditor.ViewModels
{
    public sealed class PolylineParameterViewModel : ObservableObject
    {
        private double _openTime;
        private double _closeTime;
        private int _pulse;
        private bool _isEnabled;

        public double OpenTime
        {
            get => _openTime;
            set => SetProperty(ref _openTime, value);
        }

        public double CloseTime
        {
            get => _closeTime;
            set => SetProperty(ref _closeTime, value);
        }

        public int Pulse
        {
            get => _pulse;
            set => SetProperty(ref _pulse, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public void Clear()
        {
            OpenTime = 0;
            CloseTime = 0;
            Pulse = 0;
            IsEnabled = false;
        }
    }
}
