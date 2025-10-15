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
        private double _pixelsPerMillimetre = 1.0;
        private double _centerX;
        private double _centerY;
        private int _schemaVersion = 3;
        private string _name = "Recipe Item";

        public PathRecipeItem()
        {
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

        [JsonProperty("pixelsPerMillimetre")]
        public double PixelsPerMillimetre
        {
            get => _pixelsPerMillimetre;
            set => SetProperty(ref _pixelsPerMillimetre, value <= 0 ? 1.0 : Math.Round(Clamp(value, 0.01, 100), 3));
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

        [JsonProperty("segments")]
        public ObservableCollection<PathSegment> Segments { get; }

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
