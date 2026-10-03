namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class JoypadControlScheme : Resource
{
    [Export] public JoyButton ConfirmButton { get; set; }
    [Export] public JoyButton CancelButton { get; set; }
    [Export] public JoyButton StartButton { get; set; }
}
