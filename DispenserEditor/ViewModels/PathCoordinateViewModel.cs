using System;
using DispenserEditor.Models;

namespace DispenserEditor.ViewModels;

public class PathCoordinateViewModel : ObservableObject
{
    private readonly Action<PathCoordinateViewModel, string?> _changedCallback;
    private string _name = string.Empty;
    private CoordinateType _type;
    private double _x;
    private double _y;
    private bool _useCustomSpeed;
    private double _speed;
    private double _acceleration;
    private double _deceleration;
    private bool _dispenseEnabled = true;
    private bool _useAutoSmoothing = true;
    private double _pixelX;
    private double _pixelY;
    private int _lineGroup;

    public PathCoordinateViewModel(Action<PathCoordinateViewModel, string?> changedCallback)
    {
        _changedCallback = changedCallback;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name
    {
        get => _name;
        set => UpdateProperty(ref _name, value);
    }

    public CoordinateType Type
    {
        get => _type;
        set => UpdateProperty(ref _type, value);
    }

    public double X
    {
        get => _x;
        set => UpdateCoordinate(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => UpdateCoordinate(ref _y, value);
    }

    public bool UseCustomSpeed
    {
        get => _useCustomSpeed;
        set => UpdateProperty(ref _useCustomSpeed, value);
    }

    public double Speed
    {
        get => _speed;
        set => UpdateCoordinate(ref _speed, value, round: false);
    }

    public double Acceleration
    {
        get => _acceleration;
        set => UpdateCoordinate(ref _acceleration, value, round: false);
    }

    public double Deceleration
    {
        get => _deceleration;
        set => UpdateCoordinate(ref _deceleration, value, round: false);
    }

    public bool DispenseEnabled
    {
        get => _dispenseEnabled;
        set => UpdateProperty(ref _dispenseEnabled, value);
    }

    public bool UseAutoSmoothing
    {
        get => _useAutoSmoothing;
        set => UpdateProperty(ref _useAutoSmoothing, value);
    }

    public double PixelX
    {
        get => _pixelX;
        private set => SetProperty(ref _pixelX, value);
    }

    public double PixelY
    {
        get => _pixelY;
        private set => SetProperty(ref _pixelY, value);
    }

    public int LineGroup
    {
        get => _lineGroup;
        set => UpdateProperty(ref _lineGroup, value);
    }

    public static PathCoordinateViewModel FromModel(PathCoordinate model, Action<PathCoordinateViewModel, string?> changedCallback)
    {
        return new PathCoordinateViewModel(changedCallback)
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            X = model.X,
            Y = model.Y,
            UseCustomSpeed = model.UseCustomSpeed,
            Speed = model.Speed,
            Acceleration = model.Acceleration,
            Deceleration = model.Deceleration,
            DispenseEnabled = model.DispenseEnabled,
            UseAutoSmoothing = model.UseAutoSmoothing,
            LineGroup = model.LineGroup
        };
    }

    public PathCoordinate ToModel()
    {
        return new PathCoordinate
        {
            Id = Id,
            Name = Name,
            Type = Type,
            X = Math.Round(X, 3),
            Y = Math.Round(Y, 3),
            UseCustomSpeed = UseCustomSpeed,
            Speed = Speed,
            Acceleration = Acceleration,
            Deceleration = Deceleration,
            DispenseEnabled = DispenseEnabled,
            UseAutoSmoothing = UseAutoSmoothing,
            LineGroup = LineGroup
        };
    }

    public void UpdatePixelPosition(double mmPerPixel, double centerX, double centerY)
    {
        PixelX = centerX + (X / mmPerPixel);
        PixelY = centerY - (Y / mmPerPixel);
    }

    public void UpdateFromPixel(double pixelX, double pixelY, double mmPerPixel, double centerX, double centerY)
    {
        PixelX = pixelX;
        PixelY = pixelY;
        X = (pixelX - centerX) * mmPerPixel;
        Y = (centerY - pixelY) * mmPerPixel;
    }

    private void UpdateProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            _changedCallback?.Invoke(this, propertyName);
        }
    }

    private void UpdateCoordinate(ref double field, double value, bool round = true, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        var newValue = round ? Math.Round(value, 3, MidpointRounding.AwayFromZero) : value;
        if (SetProperty(ref field, newValue, propertyName))
        {
            _changedCallback?.Invoke(this, propertyName);
        }
    }
}
