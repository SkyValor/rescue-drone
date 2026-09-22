namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class UserSettings : Resource
{
    public enum KeyboardInputType { Keyboard1, Keyboard2 }
    
    [Export] public InputDeviceType PreferredInputDevice { get; set; }
    [Export] public KeyboardInputType PreferredKeyboardInputType { get; set; }
}
