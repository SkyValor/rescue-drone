namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Godot.Collections;

[Tool]
[Meta(typeof(IAutoOn))]
public partial class Waypoint : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public Array<Waypoint> Connections { get; set; }

    private const float SPHERE_RADIUS = 0.5f;
    private readonly Color SelfColor = Colors.Orange;
    private readonly Color ConnectionColor = Colors.Cyan;
    private readonly Color LookingDirectionColor = Colors.Red;

    private Vector3 direction1 = Vector3.Zero;
    private Vector3 direction2 = Vector3.Zero;
    private Vector3 direction3 = Vector3.Zero;
    private Vector3 direction4 = Vector3.Zero;
    
    public override void _Ready()
    {
        direction1 = -Basis.Z.Rotated(Vector3.Up, Mathf.DegToRad(45f)).Normalized();
        direction2 = direction1.Rotated(Vector3.Right, Mathf.DegToRad(15f)).Normalized();

        direction3 = -Basis.Z.Rotated(Vector3.Up, Mathf.DegToRad(-45f)).Normalized();
        direction4 = direction3.Rotated(Vector3.Right, Mathf.DegToRad(15f)).Normalized();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) DrawGizmos();
    }

    private void DrawGizmos()
    {
        DebugDraw3D.DrawSphere(GlobalPosition, SPHERE_RADIUS, SelfColor);
        DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + -Basis.Z, LookingDirectionColor);
        
        DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + direction1, Colors.Blue);
        DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + direction2, Colors.DarkBlue);
        DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + direction3, Colors.White);
        DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + direction4, Colors.Gray);
    
        foreach (var connection in Connections)
            DebugDraw3D.DrawLine(GlobalPosition, connection.GlobalPosition, ConnectionColor);
    }
    
}
