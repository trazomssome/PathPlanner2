using System.Collections.Generic;

namespace DispensePath
{
    public class PathInterpolationPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Acceleration { get; set; }
        public double Deceleration { get; set; }
        public bool DispenseEnabled { get; set; }
        public bool AutoSmoothing { get; set; }
    }

    public class PathInterpolationSegment
    {
        public string Id { get; set; }
        public bool UsesCustomSpeed { get; set; }
        public bool IsContinuous { get; set; }
        public List<PathInterpolationPoint> Points { get; set; } = new List<PathInterpolationPoint>();
    }

    public class PathInterpolationRecipe
    {
        public string SchemaVersion { get; set; }
        public double DefaultSpeed { get; set; }
        public double? SpeedLimit { get; set; }
        public List<PathInterpolationSegment> Segments { get; set; } = new List<PathInterpolationSegment>();
    }
}
