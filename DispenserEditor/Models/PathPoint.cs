using System;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public class PathPoint : ObservableObject
    {
        private double _x;
        private double _y;
        private double _speed = 100;
        private double _acceleration = 200;
        private double _deceleration = 200;
        private bool _dispenseEnabled = true;
        private bool _autoSmoothing = true;
        private bool _useCustomSpeed;
        private bool _isSingle = true;
        private int _lineGroup;

        [JsonProperty("x")]
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, Math.Round(value, 3));
        }

        [JsonProperty("y")]
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, Math.Round(value, 3));
        }

        [JsonProperty("speed")]
        public double Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, Math.Round(Clamp(value, 0.1, 1000), 3));
        }

        [JsonProperty("acceleration")]
        public double Acceleration
        {
            get => _acceleration;
            set => SetProperty(ref _acceleration, Math.Round(Clamp(value, 0.1, 5000), 3));
        }

        [JsonProperty("deceleration")]
        public double Deceleration
        {
            get => _deceleration;
            set => SetProperty(ref _deceleration, Math.Round(Clamp(value, 0.1, 5000), 3));
        }

        [JsonProperty("dispense")]        
        public bool DispenseEnabled
        {
            get => _dispenseEnabled;
            set => SetProperty(ref _dispenseEnabled, value);
        }

        [JsonProperty("autoSmoothing")]
        public bool AutoSmoothing
        {
            get => _autoSmoothing;
            set => SetProperty(ref _autoSmoothing, value);
        }

        [JsonProperty("useCustomSpeed")]
        public bool UseCustomSpeed
        {
            get => _useCustomSpeed;
            set => SetProperty(ref _useCustomSpeed, value);
        }

        [JsonProperty("isSingle")]
        public bool IsSingle
        {
            get => _isSingle;
            set => SetProperty(ref _isSingle, value);
        }

        [JsonProperty("lineGroup")]
        public int LineGroup
        {
            get => _lineGroup;
            set => SetProperty(ref _lineGroup, value);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
