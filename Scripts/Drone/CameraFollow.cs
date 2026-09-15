namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn))]
public partial class CameraFollow : Camera3D
{
    [Export] public Node3D SpringArm { get; private set; }
    [Export] public float LerpPower { get; private set; } = 1f;

    public override void _Process(double delta)
    {
        Position = Position.Lerp(SpringArm.Position, LerpPower * (float) delta);
    }
}
