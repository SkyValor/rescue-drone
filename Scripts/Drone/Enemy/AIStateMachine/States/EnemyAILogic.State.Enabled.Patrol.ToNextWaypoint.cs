namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drones attempts to get the next waypoint from the current position in the circuit.
        /// To do so, the drone generates a pathway towards it and slowly flies along it.
        ///
        /// When reaching the target waypoint, it changes the state to <see cref="State.Scanning"/>.
        /// </summary>
        [Meta]
        public partial record ToNextWaypoint : Patrol
        {
            public ToNextWaypoint()
            {
                this.OnEnter(() =>
                {
                    TryGettingNextWaypoint();
                    GeneratePathToWaypoint();
                });
            }

            private void TryGettingNextWaypoint()
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
            }

            private void GeneratePathToWaypoint()
            {
                var data = Get<Data>();
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
                    data.CurrentPathIndex = 1; // We skip the first point since it's where we are at
                }
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                ComputeMovementToWaypoint(input.Delta);
                return ToSelf();
            }
            
        }
    }
}
