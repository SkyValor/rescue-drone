namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class SightSensor : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    public event Action<IFlyingDrone> DroneOnSight;
    
    public enum DroneWatchType { Player, Enemy }

    [Export] public DroneWatchType WatchType { get; private set; }
    [Export] public float DepthRange { get; private set; }
    [Export] public float VisionRange { get; private set; }

    [Dependency] public IGameRepo GameRepo => this.DependOn<IGameRepo>();
    [Node] private RayCast3D VisionRaycast { get; set; }
    
    public void OnReady()
    {
        if (VisionRaycast is not null) return;
        
        var raycast = new RayCast3D();
        AddChild(raycast);
        VisionRaycast = raycast;
    }
    
    public void DetectDrones()
    {
        if (WatchType is DroneWatchType.Player)
        {
            var playerDrone = GameRepo.PlayerDrone.Value;
            if (playerDrone is null) return;
            
            if (TargetInSight(playerDrone)) 
                DroneOnSight?.Invoke(playerDrone);
        }
        else if (WatchType is DroneWatchType.Enemy)
        {
            var enemyDrones = GameRepo.EnemyDrones.Value;
            if (enemyDrones is null || enemyDrones.Length == 0) return;
            
            foreach (var enemyDrone in enemyDrones)
            {
                if (TargetInSight(enemyDrone)) 
                    DroneOnSight?.Invoke(enemyDrone);
            }
        }
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
        return Mathf.RadToDeg(directionToPlayer.AngleTo(forward)) <= VisionRange / 2;
    }

    public bool NoBuildingInBetween(Node3D target)
    {
        VisionRaycast.LookAt(target.GlobalPosition, Vector3.Up);
        VisionRaycast.ForceRaycastUpdate();
        return !VisionRaycast.IsColliding();
    }
    
}
