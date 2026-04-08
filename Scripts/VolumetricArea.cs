namespace RescueDrone.Scripts;

using Godot;

public partial class VolumetricArea : Node3D
{
    [Export] public Vector3 VolumetricAreaDimensions = Vector3.Zero;

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawBox(GlobalPosition, Quaternion.Identity, VolumetricAreaDimensions);
    }
}
