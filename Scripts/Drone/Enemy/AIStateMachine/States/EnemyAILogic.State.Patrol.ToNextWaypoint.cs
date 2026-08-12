namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record ToNextWaypoint : Patrol, IGet<Input.PhysicsTick>
        {
            private ToNextWaypoint()
            {
                this.OnEnter(() =>
                {
                    // Get the next waypoint to travel to
                    var data = Get<Data>();
                    if (data.CurrentWaypoint is null)
                    {
                        GD.PrintErr("Waypoint is set as null in data. Fallback to Idle state.");
                        Input(new Input.ReturnToIdle());
                        return;
                    }
                    if (data.CurrentWaypoint.Connections.Count == 0)
                    {
                        GD.PrintErr("Waypoint does not have connections. Fallback to Idle state.");
                        Input(new Input.ReturnToIdle());
                        return;
                    }
                    
                    data.CurrentWaypoint = data.CurrentWaypoint.Connections.PickRandom();
                    
                    // Use the Drone Pathfinder to find the best path to that waypoint
                    var enemy = Get<EnemyAIDrone>();
                    var origin = enemy.GlobalPosition;
                    var target = data.CurrentWaypoint.GlobalPosition;

                    var path = GeneratePathway(origin, target);
                    if (path.Length == 0)
                    {
                        GD.PrintErr("Pathfinder did not find a path to the next waypoint. Fallback to Idle state.");
                        Input(new Input.ReturnToIdle());
                    }
                    else
                    {
                        data.SVOPath = path;
                        data.CurrentPathIndex = 1;
                    }
                });
            }

            public Transition On(in Input.PhysicsTick input)
            {
                ComputeMovementToWaypoint(input.Delta);
                return ToSelf();
            }
        }
    }
}
