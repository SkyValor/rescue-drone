namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn))]
public partial class SightSensor : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public float DepthRange { get; private set; }
    [Export] public float VisionRange { get; private set; }
    
    [Node] private RayCast3D VisionRaycast { get; set; }

    public void OnReady()
    {
        if (VisionRaycast is not null) return;
        
        var raycast = new RayCast3D();
        AddChild(raycast);
        VisionRaycast = raycast;
    }

    public bool TargetInSight(Node3D target)
    {
        return 
            TargetInRange(target) && 
            TargetInVisionRange(target) && 
            NoBuildingInBetween(target);
    }

    public bool TargetInRange(Node3D target)
    {
        var distanceToPlayer = GlobalPosition.DistanceTo(target.GlobalPosition);
        return distanceToPlayer <= DepthRange;
    }

    public bool TargetInVisionRange(Node3D target)
    {
        var forward = -Basis.Z;
        var directionToPlayer = GlobalPosition.DirectionTo(target.GlobalPosition);
        return Mathf.RadToDeg(directionToPlayer.AngleTo(forward)) <= 90f / 2;
    }

    public bool NoBuildingInBetween(Node3D target)
    {
        VisionRaycast.LookAt(target.GlobalPosition, Vector3.Up);
        VisionRaycast.ForceRaycastUpdate();
        return !VisionRaycast.IsColliding();
    }
}
