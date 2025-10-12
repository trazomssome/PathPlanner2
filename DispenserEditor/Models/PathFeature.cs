using System;
using System.Collections.ObjectModel;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public enum PathFeatureType
    {
        Point,
        Line
    }

    public class PathFeature : ObservableObject
    {
        private string _name = "New Feature";
        private bool _isSelected;
        private PathFeatureType _type;
        private bool _useCustomSpeed;
        private double _speed = 100;
        private double _acceleration = 200;
        private double _deceleration = 200;
        private bool _dispense = true;
        private bool _autoSmoothing = true;

        public PathFeature()
        {
            Points = new ObservableCollection<PathPoint>();
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [JsonProperty("type")]
        public PathFeatureType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        [JsonProperty("useCustomSpeed")]
        public bool UseCustomSpeed
        {
            get => _useCustomSpeed;
            set => SetProperty(ref _useCustomSpeed, value);
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
        public bool Dispense
        {
            get => _dispense;
            set => SetProperty(ref _dispense, value);
        }

        [JsonProperty("autoSmoothing")]
        public bool AutoSmoothing
        {
            get => _autoSmoothing;
            set => SetProperty(ref _autoSmoothing, value);
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        [JsonProperty("points")]
        public ObservableCollection<PathPoint> Points { get; }

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
