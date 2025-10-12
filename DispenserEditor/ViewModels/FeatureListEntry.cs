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

        private FeatureListEntry(PathFeature feature, PathPoint startPoint, PathPoint endPoint, int segmentIndex)
        {
            Feature = feature;
            StartPoint = startPoint;
            EndPoint = endPoint;
            SegmentIndex = segmentIndex;

            if (StartPoint != null)
            {
                StartPoint.PropertyChanged += OnPointPropertyChanged;
            }

            if (EndPoint != null)
            {
                EndPoint.PropertyChanged += OnPointPropertyChanged;
            }
        }

        public PathFeature Feature { get; }

        public PathPoint StartPoint { get; }

        public PathPoint EndPoint { get; }

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

        public string TypeDescription => Feature.Type == PathFeatureType.Line ? "선" : "점";

        public double? StartX
        {
            get => StartPoint?.X;
            set
            {
                if (StartPoint != null && value.HasValue)
                {
                    StartPoint.X = value.Value;
                    RaisePropertyChanged(nameof(StartX));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public double? StartY
        {
            get => StartPoint?.Y;
            set
            {
                if (StartPoint != null && value.HasValue)
                {
                    StartPoint.Y = value.Value;
                    RaisePropertyChanged(nameof(StartY));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public double? EndX
        {
            get => EndPoint?.X;
            set
            {
                if (EndPoint != null && value.HasValue)
                {
                    EndPoint.X = value.Value;
                    RaisePropertyChanged(nameof(EndX));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public double? EndY
        {
            get => EndPoint?.Y;
            set
            {
                if (EndPoint != null && value.HasValue)
                {
                    EndPoint.Y = value.Value;
                    RaisePropertyChanged(nameof(EndY));
                    RaisePropertyChanged(nameof(Coordinates));
                }
            }
        }

        public bool HasEndPoint => EndPoint != null;

        public string Coordinates
        {
            get
            {
                if (Feature.Type == PathFeatureType.Line && StartPoint != null && EndPoint != null)
                {
                    return $"시작({Format(StartPoint.X)}, {Format(StartPoint.Y)}) → 끝({Format(EndPoint.X)}, {Format(EndPoint.Y)})";
                }

                if (StartPoint != null)
                {
                    return $"({Format(StartPoint.X)}, {Format(StartPoint.Y)})";
                }

                return string.Empty;
            }
        }

        public static FeatureListEntry ForPoint(PathFeature feature, PathPoint point)
        {
            return new FeatureListEntry(feature, point, null, -1);
        }

        public static FeatureListEntry ForSegment(PathFeature feature, PathPoint startPoint, PathPoint endPoint, int segmentIndex)
        {
            return new FeatureListEntry(feature, startPoint, endPoint, segmentIndex);
        }

        public void Dispose()
        {
            if (StartPoint != null)
            {
                StartPoint.PropertyChanged -= OnPointPropertyChanged;
            }

            if (EndPoint != null)
            {
                EndPoint.PropertyChanged -= OnPointPropertyChanged;
            }
        }

        private static string Format(double value)
        {
            return value.ToString("F3", Culture);
        }

        private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == StartPoint)
            {
                if (e.PropertyName == nameof(PathPoint.X))
                {
                    RaisePropertyChanged(nameof(StartX));
                }
                else if (e.PropertyName == nameof(PathPoint.Y))
                {
                    RaisePropertyChanged(nameof(StartY));
                }
            }
            else if (sender == EndPoint)
            {
                if (e.PropertyName == nameof(PathPoint.X))
                {
                    RaisePropertyChanged(nameof(EndX));
                }
                else if (e.PropertyName == nameof(PathPoint.Y))
                {
                    RaisePropertyChanged(nameof(EndY));
                }
            }

            RaisePropertyChanged(nameof(Coordinates));
        }
    }
}
