namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn))]
public partial class SightSensor : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<Vector3> PlayerInSight;
    public event Action LostSightOfPlayer;
    
    [Export] public float DepthRange { get; private set; }
    [Export] public float VisionRange { get; private set; }
    
    [Node] private RayCast3D VisionRaycast { get; set; }

    private PlayerMover playerTracked;
    private bool inSight;

    public void OnReady()
    {
        if (VisionRaycast is not null) return;
        
        var raycast = new RayCast3D();
        AddChild(raycast);
        VisionRaycast = raycast;
    }

    public void OnPhysicsProcess(double delta)
    {
        if (playerTracked is null) return;

        if (TargetInSight(playerTracked))
        {
            inSight = true;
            PlayerInSight?.Invoke(playerTracked.GlobalPosition);
        }
        else if (inSight)
        {
            inSight = false;
            LostSightOfPlayer?.Invoke();
        }
    }

    // TODO: In the future, make this track any drone!!
    
    public void TrackPlayer(PlayerMover player) => playerTracked = player;
    public void StopTracking() => playerTracked = null;

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
