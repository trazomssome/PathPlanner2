using System;
using System.ComponentModel;
using System.Globalization;
using DispenserEditor.Infrastructure;
using DispenserEditor.Models;

namespace DispenserEditor.ViewModels
{
    public class FeatureListEntry : ObservableObject, IDisposable
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        private FeatureListEntry(PathFeature feature, PathPoint point, int segmentIndex)
        {
            Feature = feature;
            Point = point;
            SegmentIndex = segmentIndex;

            if (Point != null)
            {
                Point.PropertyChanged += OnPointPropertyChanged;
            }
        }

        public PathFeature Feature { get; }

        public PathPoint Point { get; }

        public int SegmentIndex { get; }

        public string DisplayName
        {
            get
            {
                if (Feature.Type == PathFeatureType.Line && SegmentIndex >= 0)
                {
                    return $"{Feature.Name} (Segment {SegmentIndex + 1})";
                }

                return Feature.Name;
            }
        }

        public string TypeDescription => Feature.Type == PathFeatureType.Line ? "Line" : "Point";

        public double? X
        {
            get => Point?.X;
            set
            {
                if (Point != null && value.HasValue)
                {
                    Point.X = value.Value;
                    RaisePropertyChanged(nameof(X));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public double? Y
        {
            get => Point?.Y;
            set
            {
                if (Point != null && value.HasValue)
                {
                    Point.Y = value.Value;
                    RaisePropertyChanged(nameof(Y));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public string Coordinates
        {
            get
            {
                if (Point != null)
                {
                    return $"({Format(Point.X)}, {Format(Point.Y)})";
                }

                return string.Empty;
            }
        }

        public static FeatureListEntry ForPoint(PathFeature feature, PathPoint point)
        {
            return new FeatureListEntry(feature, point, -1);
        }

        public static FeatureListEntry ForLinePoint(PathFeature feature, PathPoint point, int segmentIndex)
        {
            return new FeatureListEntry(feature, point, segmentIndex);
        }

        public void Dispose()
        {
            if (Point != null)
            {
                Point.PropertyChanged -= OnPointPropertyChanged;
            }
        }

        private static string Format(double value)
        {
            return value.ToString("F3", Culture);
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == Point)
            {
                if (e.PropertyName == nameof(PathPoint.X))
                {
                    RaisePropertyChanged(nameof(X));
                }
                else if (e.PropertyName == nameof(PathPoint.Y))
                {
                    RaisePropertyChanged(nameof(Y));
                }
            }

            RaisePropertyChanged(nameof(Coordinates));
        }
    }
}
