using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DispenserEditor.Models;

public class DispenseRecipe : ICloneable
{
    public string Name { get; set; } = "기본 레시피";
    public double DefaultSpeed { get; set; } = 100.0;
    public bool SpeedLimitEnabled { get; set; }
    public double SpeedLimit { get; set; } = 500.0;
    public double Opacity { get; set; } = 0.85;
    public ObservableCollection<PathCoordinate> Coordinates { get; set; } = new();

    public object Clone()
    {
        var clone = (DispenseRecipe)MemberwiseClone();
        clone.Coordinates = new ObservableCollection<PathCoordinate>(
            Coordinates.Select(c => (PathCoordinate)c.Clone()));
        return clone;
    }
}
