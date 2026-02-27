namespace RescueDrone;

using Godot;
using Godot.Collections;

[Tool]
public partial class Waypoint : Node3D
{
    [Export] public Array<Waypoint> Connections { get; set; }

    private float sphereRadius = 0.5f;
    private Color sphereColor = Colors.Orange;
    private Color lineColor = Colors.Cyan;

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawSphere(GlobalPosition, 0.5f, Colors.Orange);
        
        foreach (var connection in Connections)
            DebugDraw3D.DrawLine(GlobalPosition, connection.GlobalPosition, lineColor);
    }
    
}