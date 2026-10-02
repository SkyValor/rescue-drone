namespace RescueDrone;

using System;
using Godot;

[GlobalClass]
public partial class UserSettings : Resource
{
    [Export] public InputType PreferredInputDevice { get; set; }
    [Export] public InputDeviceScheme PreferredInputDeviceScheme { get; set; }
    
    [Export] public SensitivitySettings MouseSensitivity { get; private set; }
    [Export] public SensitivitySettings KeyboardSensitivity { get; private set; }
    [Export] public SensitivitySettings AnalogSensitivity { get; private set; }

    [Export(PropertyHint.Range, "1, 10, 1")]
    public int AnalogSensitivityIndex { get; set; }

    public SensitivitySettings GetSensitivitySettings(InputType forType)
    {
        return forType switch
        {
            InputType.KeyboardAndMouse => MouseSensitivity,
            InputType.KeyboardOnly     => KeyboardSensitivity,
            InputType.Joypad           => AnalogSensitivity,
            _                          => throw new ArgumentOutOfRangeException(nameof(forType), forType, null)
        };
    }
    
}
