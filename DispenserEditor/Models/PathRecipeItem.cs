using System;
using System.Collections.ObjectModel;
using DispenserEditor.Infrastructure;
using Newtonsoft.Json;

namespace DispenserEditor.Models
{
    public class PathRecipeItem : ObservableObject
    {
        private double _defaultSpeed = 10.0;
        private double _overlayOpacity = 0.8;
        private double _millimetresPerPixel = 1.0;
        private double _centerX;
        private double _centerY;
        private int _schemaVersion = 1;
        private string _name = "Recipe Item";
        private bool _isXMirrored;
        private bool _isYMirrored;

        public PathRecipeItem()
        {
            Features = new ObservableCollection<PathFeature>();
            Segments = new ObservableCollection<PathSegment>();
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [JsonProperty("defaultSpeed")]
        public double DefaultSpeed
        {
            get => _defaultSpeed;
            set => SetProperty(ref _defaultSpeed, Math.Round(Clamp(value, 0.1, 1000), 3));
        }

        [JsonProperty("overlayOpacity")]
        public double OverlayOpacity
        {
            get => _overlayOpacity;
            set => SetProperty(ref _overlayOpacity, Clamp(value, 0.0, 1.0));
        }

        [JsonProperty("millimetresPerPixel")]
        public double MillimetresPerPixel
        {
            get => _millimetresPerPixel;
            set => SetProperty(ref _millimetresPerPixel, value <= 0 ? 1.0 : Math.Round(Clamp(value, 0.01, 100), 3));
        }

        [JsonProperty("pixelsPerMillimetre")]
        private double LegacyPixelsPerMillimetre
        {
            set => MillimetresPerPixel = value > 0 ? 1.0 / value : 1.0;
        }

        [JsonProperty("centerX")]
        public double CenterX
        {
            get => _centerX;
            set => SetProperty(ref _centerX, Math.Round(value, 3));
        }

        [JsonProperty("centerY")]
        public double CenterY
        {
            get => _centerY;
            set => SetProperty(ref _centerY, Math.Round(value, 3));
        }

        [JsonProperty("schemaVersion")]
        public int SchemaVersion
        {
            get => _schemaVersion;
            set => SetProperty(ref _schemaVersion, value);
        }

        [JsonProperty("features")]
        public ObservableCollection<PathFeature> Features { get; }

        [JsonProperty("segments")]
        public ObservableCollection<PathSegment> Segments { get; }

        [JsonProperty("isXMirrored")]
        public bool IsXMirrored
        {
            get => _isXMirrored;
            set => SetProperty(ref _isXMirrored, value);
        }

        [JsonProperty("isYMirrored")]
        public bool IsYMirrored
        {
            get => _isYMirrored;
            set => SetProperty(ref _isYMirrored, value);
        }

        public void FlipXCoordinates()
        {
            foreach (var segment in Segments)
            {
                segment.X = Math.Round(-segment.X, 3);
            }

            foreach (var feature in Features)
            {
                foreach (var point in feature.Points)
                {
                    point.X = Math.Round(-point.X, 3);
                }
            }

            IsXMirrored = !IsXMirrored;
        }

        public void FlipYCoordinates()
        {
            foreach (var segment in Segments)
            {
                segment.Y = Math.Round(-segment.Y, 3);
            }

            foreach (var feature in Features)
            {
                foreach (var point in feature.Points)
                {
                    point.Y = Math.Round(-point.Y, 3);
                }
            }

            IsYMirrored = !IsYMirrored;
        }

        public double ToDisplayX(double storedX)
        {
            return IsXMirrored ? Math.Round(-storedX, 3) : storedX;
        }

        public double ToDisplayY(double storedY)
        {
            return IsYMirrored ? Math.Round(-storedY, 3) : storedY;
        }

        public double ToStoredX(double displayX)
        {
            return IsXMirrored ? Math.Round(-displayX, 3) : Math.Round(displayX, 3);
        }

        public double ToStoredY(double displayY)
        {
            return IsYMirrored ? Math.Round(-displayY, 3) : Math.Round(displayY, 3);
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
