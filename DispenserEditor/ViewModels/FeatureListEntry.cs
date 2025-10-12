using System.Globalization;
using DispenserEditor.Infrastructure;
using DispenserEditor.Models;

namespace DispenserEditor.ViewModels
{
    public class FeatureListEntry : ObservableObject
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        private FeatureListEntry(PathFeature feature, PathPoint startPoint, PathPoint endPoint, int segmentIndex)
        {
            Feature = feature;
            StartPoint = startPoint;
            EndPoint = endPoint;
            SegmentIndex = segmentIndex;
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

        private static string Format(double value)
        {
            return value.ToString("F3", Culture);
        }
    }
}
