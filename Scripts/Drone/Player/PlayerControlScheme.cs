namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class PlayerControlScheme : Resource
{
    public enum KeyControl { KeyboardAndMouse, Controller }
    
    [Export] public KeyControl ControlType { get; private set; }
    [Export] public string Name { get; private set; }
    [Export] public string Description { get; private set; }
    
    [Export] public Key ThrottleUp { get; private set; }
    [Export] public Key ThrottleDown { get; private set; }
}
