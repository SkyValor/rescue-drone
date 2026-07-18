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

    private float sphereRadius = 0.5f;
    private Color sphereColor = Colors.Orange;
    private Color lineColor = Colors.Cyan;

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;
        
        DebugDraw3D.DrawSphere(GlobalPosition, 0.5f, sphereColor);
    
        foreach (var connection in Connections)
            DebugDraw3D.DrawLine(GlobalPosition, connection.GlobalPosition, lineColor);
    }
    
}
