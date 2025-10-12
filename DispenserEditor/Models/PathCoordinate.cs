using System;

namespace DispenserEditor.Models
{
    public class PathCoordinate : ICloneable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public CoordinateType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool UseCustomSpeed { get; set; }
        public double Speed { get; set; }
        public double Acceleration { get; set; }
        public double Deceleration { get; set; }
        public bool DispenseEnabled { get; set; } = true;
        public bool UseAutoSmoothing { get; set; } = true;
        public int LineGroup { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

}