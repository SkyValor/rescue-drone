namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class UserSettings : Resource
{
    [Export] public InputDeviceType PreferredInputDevice { get; set; }
    [Export] public InputDeviceScheme PreferredInputDeviceScheme { get; set; }
    
    [Export] public bool RotateCameraWithMouse { get; set; }
}
