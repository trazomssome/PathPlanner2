using System;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public enum PathSegmentType
    {
        Point,
        Line
    }

    public class PathSegment : ObservableObject
    {
        private PathSegmentType _segmentType = PathSegmentType.Point;
        private bool _onDispensing = true;
        private int _lineGroup;
        private double _x;
        private double _y;
        private double _speed = 100;
        private double _acceleration = 200;
        private double _deceleration = 200;
        private bool _autoSmoothing = true;

        [JsonProperty("segmentType")]
        public PathSegmentType SegmentType
        {
            get => _segmentType;
            set => SetProperty(ref _segmentType, value);
        }

        [JsonProperty("onDispensing")]
        public bool OnDispensing
        {
            get => _onDispensing;
            set => SetProperty(ref _onDispensing, value);
        }

        [JsonProperty("lineGroup")]
        public int LineGroup
        {
            get => _lineGroup;
            set => SetProperty(ref _lineGroup, value);
        }

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

        [JsonProperty("autoSmoothing")]
        public bool AutoSmoothing
        {
            get => _autoSmoothing;
            set => SetProperty(ref _autoSmoothing, value);
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
