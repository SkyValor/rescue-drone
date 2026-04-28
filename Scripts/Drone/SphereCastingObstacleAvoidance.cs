namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class SphereCastingObstacleAvoidance : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node] private ShapeCast3D SphereCast { get; set; }
    
    public bool CheckObstacleInDirection(Vector3 direction)
    {
        SphereCast.LookAt(direction, Vector3.Up);
        SphereCast.ForceShapecastUpdate();
        return SphereCast.IsColliding();
    }
    
}
