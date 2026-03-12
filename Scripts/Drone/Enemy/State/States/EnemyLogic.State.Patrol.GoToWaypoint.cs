namespace RescueDrone;

using System;
using Chickensoft.LogicBlocks;
using Godot;
using Godot.Collections;

public partial class EnemyLogic
{
    public partial record State
    {
        public partial record Patrol
        {
            public record GoToWaypoint : Patrol, IGet<Input.PhysicsTick>
            {
                public GoToWaypoint()
                {
                    this.OnEnter(() =>
                    {
                        GD.Print("Patrol state - Go to waypoint");
                        var droneMovement = Get<IEnemyDrone>().DroneMovement;
                        
                        // If the enemy drone is starting the patrol state,
                        // get the closest waypoint
                        if (currentWaypoint is null)
                        {
                            currentWaypoint = GetClosestWaypoint();
                            if (currentWaypoint is null)
                            {
                                AddError(new MissingFieldException("Enemy drone state machine cannot get closest waypoint."));
                                return;
                            }
                            
                            GD.Print("First waypoint: " + currentWaypoint.GlobalPosition);
                            // droneMovement.SeekTarget(currentWaypoint);
                            return;
                        }
                        
                        // Otherwise, from this waypoint find a valid connection
                        var nextWaypoint = GetNextWaypoint(); // TODO: Can return null
                        previousWaypoint = currentWaypoint;
                        currentWaypoint = nextWaypoint;
                        // droneMovement.SeekTarget(currentWaypoint);
                    });
                }
                
                public Transition On(in Input.PhysicsTick input)
                {
                    var drone = Get<IEnemyDrone>();
                    var distanceToWaypoint = drone.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
                    if (distanceToWaypoint < 0.1f)
                    {
                        // If we are coming from another waypoint, start the lookout phase
                        if (previousWaypoint is not null)
                            return To<Lookout>();
                        
                        // Otherwise move to a connection waypoint
                        var nextWaypoint = GetNextWaypoint();
                        previousWaypoint = currentWaypoint;
                        currentWaypoint = nextWaypoint;
                        GD.Print("Next waypoint: " + currentWaypoint.GlobalPosition);
                        return ToSelf();
                    }
                    
                    var targetPosition = currentWaypoint.GlobalPosition;
                    var direction = targetPosition - drone.GlobalPosition;
                    Output(new Output.MovementRequest(direction, input.DeltaTime));
                    return ToSelf();
                }
                
                private Waypoint GetClosestWaypoint()
                {
                    var drone = Get<IEnemyDrone>();
                    var waypoints = Get<Array<Waypoint>>();
                    Waypoint closestWaypoint = null;
                    var distanceToWaypoint = float.MaxValue;
                    foreach (var waypoint in waypoints)
                    {
                        var distance = drone.GlobalPosition.DistanceTo(waypoint.GlobalPosition);
                        if (distance >= distanceToWaypoint) 
                            continue;
				
                        distanceToWaypoint = distance;
                        closestWaypoint = waypoint;
                    }
			
                    return closestWaypoint;
                }
                
                private Waypoint GetNextWaypoint()
                {
                    var connections = currentWaypoint.Connections.Duplicate();
                    if (previousWaypoint is not null)
                        connections.Remove(previousWaypoint);

                    return connections.Count == 0 ? previousWaypoint : connections.PickRandom();
                }
                
            }
        }
    }
}
