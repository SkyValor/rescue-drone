namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IWaypointCircuit : INode3D
{
    Waypoint[] Waypoints { get; }
    
    bool IsFreeToPatrol();
    void SetPatrolling(Mover drone);
    void RemovePatrolling();
    Waypoint GetClosestWaypoint(Vector3 toPosition);
    Waypoint NextWaypoint();
}

[Meta(typeof(IAutoNode))]
public partial class WaypointCircuit : Node3D, IWaypointCircuit
{
    public override void _Notification(int what) => this.Notify(what);

    public Waypoint[] Waypoints { get; private set; }
    private Waypoint cachedWaypoint;
    private Mover droneInCircuit;

    public void OnReady()
    {
        var waypoints = new List<Waypoint>();
        foreach (var child in GetChildren())
        {
            if (child is not Waypoint wp) continue;
            waypoints.Add(wp);
        }

        Waypoints = waypoints.ToArray();
    }

    public bool IsFreeToPatrol() => droneInCircuit is null;

    public void SetPatrolling(Mover drone)
    {
        if (droneInCircuit is not null && droneInCircuit != drone)
        {
            GD.PrintErr("Another drone is already patrolling this circuit.");
            return;
        }
        
        droneInCircuit = drone;
    }
    
    public void RemovePatrolling() => droneInCircuit = null;

    public Waypoint NextWaypoint() => cachedWaypoint?.Connections.PickRandom();
    
    public Waypoint GetClosestWaypoint(Vector3 toPosition)
    {
        if (Waypoints is null || Waypoints.Length == 0)
        {
            GD.PrintErr("There are no waypoints in circuit.");
            return null;
        }
        
        float shortestDistance = float.MaxValue;
        Waypoint closestWaypoint = null;
        foreach (var currentWaypoint in Waypoints)
        {
            var currentDistance = currentWaypoint.GlobalPosition.DistanceSquaredTo(toPosition);
            if (currentDistance > shortestDistance)
                continue;

            shortestDistance = currentDistance;
            closestWaypoint = currentWaypoint;
        }
        
        cachedWaypoint = closestWaypoint;
        return closestWaypoint;
    }

}
