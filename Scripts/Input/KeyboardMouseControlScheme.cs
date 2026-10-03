namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class KeyboardMouseControlScheme : Resource
{
    [Export] public Key ConfirmKey { get; set; }
    [Export] public Key CancelKey { get; set; }
    [Export] public Key PauseKey { get; set; }
    
    [Export] public Key ForwardKey { get; set; }
    [Export] public Key LeftKey { get; set; }
    [Export] public Key BackKey { get; set; }
    [Export] public Key RightKey { get; set; }
    
    [Export] public Key UpKey { get; set; }
    [Export] public Key DownKey { get; set; }
    
    [Export] public Key WeaponKey { get; set; }
    
    [Export] public Key CameraLeftKey { get; set; }
    [Export] public Key CameraRightKey { get; set; }
    [Export] public MouseButton CameraZoomInButton { get; set; }
    [Export] public MouseButton CameraZoomOutButton { get; set; }
}
