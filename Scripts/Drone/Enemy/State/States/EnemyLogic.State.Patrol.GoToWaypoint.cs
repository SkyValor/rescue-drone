namespace RescueDrone;

using Chickensoft.LogicBlocks;

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
                    this.OnEnter(() => Godot.GD.Print("GoToWaypoint"));
                }
                
                public Transition On(in Input.PhysicsTick input)
                {
                    var enemy = Get<IEnemyDrone>();
                    var distanceToWaypoint = enemy.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);

                    if (distanceToWaypoint < 0.05f)
                        return To<Lookout>();
                    
                    Output(new Output.MoveTo(currentWaypoint.GlobalPosition, input.DeltaTime));
                    return ToSelf();
                    
                    // var distanceToWaypoint = enemy.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
                    // if (distanceToWaypoint < 0.1f)
                    // {
                    //     // If we are coming from another waypoint, start the lookout phase
                    //     if (previousWaypoint is not null)
                    //         return To<Lookout>();
                    //     
                    //     // Otherwise move to a connection waypoint
                    //     var nextWaypoint = GetNextWaypoint();
                    //     previousWaypoint = currentWaypoint;
                    //     currentWaypoint = nextWaypoint;
                    //     GD.Print("Next waypoint: " + currentWaypoint.GlobalPosition);
                    //     return ToSelf();
                    // }
                    //
                    // var targetPosition = currentWaypoint.GlobalPosition;
                    // var direction = targetPosition - enemy.GlobalPosition;
                    // Output(new Output.MovementRequest(direction, input.DeltaTime));
                    // return ToSelf();
                }
                
            }
        }
    }
}
