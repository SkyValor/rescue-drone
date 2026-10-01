namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class UserSettings : Resource
{
    [Export] public InputDeviceType PreferredInputDevice { get; set; }
    [Export] public InputDeviceScheme PreferredInputDeviceScheme { get; set; }
    [Export] public bool RotateCameraWithMouse { get; set; }
    
    [Export] public SensitivitySettings MouseSensitivity { get; set; }
    [Export] public SensitivitySettings AnalogSensitivity { get; set; }

    [Export(PropertyHint.Range, "1, 10, 1")]
    public int AnalogSensitivityIndex { get; set; }
}
