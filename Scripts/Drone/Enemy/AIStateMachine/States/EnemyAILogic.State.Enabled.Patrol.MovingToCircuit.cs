namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// This is the initial state inside the <see cref="State.Patrol"/> superstate.
        ///
        /// The enemy drone moves towards the <see cref="WaypointCircuit"/> that is currently registered to,
        /// by generating a point path towards the nearest waypoint in that circuit and setting it as the target
        /// in this pathway.
        /// </summary>
        [Meta]
        public partial record MovingToCircuit : Patrol, IGet<Input.ReturnToIdle>, IGet<Input.MoveToWaypoint>
        {
            public MovingToCircuit()
            {
                this.OnEnter(() =>
                {
                    var enemy = Get<EnemyAIDrone>();
                    var data = Get<Data>();
                    if (data.CurrentCircuit is null)
                    {
                        // Get the nearest free circuit to be patrolled
                        var gameRepo = Get<IGameRepo>();
                        var circuits = gameRepo.WaypointCircuits.Value;
                        var circuit = GetClosestFreeCircuit(enemy.GlobalPosition, circuits);
                        if (circuit is null)
                        {
                            GD.Print("There are no free circuits for the enemy drone to patrol.");
                            Input(new Input.ReturnToIdle());
                            return;
                        }

                        circuit.SetPatrolling(enemy);
                        data.CurrentCircuit = circuit;
                    }

                    // Set the closest waypoint of this circuit to be the starting point in patrol state
                    var targetWaypoint = data.CurrentCircuit.GetClosestWaypoint(enemy.GlobalPosition);
                    data.CurrentWaypoint = targetWaypoint;
                    FindPathToClosestWaypoint(enemy, data);
                });
            }

            public Transition On(in Input.ReturnToIdle input) => To<Idle>();

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                
                var targetPosition = data.SVOPath[data.CurrentPathIndex];
                var isLastPoint = data.CurrentPathIndex == data.SVOPath.Length - 1;
                if (isLastPoint && enemy.GlobalPosition.IsEqualApprox(targetPosition))
                {
                    // We have reached the circuit. Move to another waypoint before scanning.
                    Input(new Input.MoveToWaypoint());
                    return ToSelf();
                }

                if (!isLastPoint && enemy.GlobalPosition.DistanceTo(targetPosition) < settings.CheckpointRadius)
                {
                    // We are close enough to consider reaching this point and can now move
                    // to the following point. We do this early to anticipate the curve and have a more
                    // realistic behavior.
                    data.CurrentPathIndex++;
                    return ToSelf();
                }
                
                ComputeMovementAlongPath(enemy, targetPosition, settings.MaxSpeed, (float) input.Delta);
                return ToSelf();
            }

            public Transition On(in Input.MoveToWaypoint input) => To<ToNextWaypoint>();

            private void FindPathToClosestWaypoint(EnemyAIDrone enemy, Data data)
            {
                var origin = enemy.GlobalPosition;
                var target = data.CurrentWaypoint.GlobalPosition;

                var path = GeneratePathway(origin, target);
                if (path.Length > 0)
                {
                    data.SVOPath = path;
                    data.CurrentPathIndex = 1; // Skip current position, which might not be the center of this node
                }
                else
                {
                    GD.Print("Something went wrong with the pathfinding algorithm. Fallback to Idle state.");
                    Input(new Input.ReturnToIdle());
                }
            }

            private static WaypointCircuit GetClosestFreeCircuit(Vector3 selfPosition, WaypointCircuit[] circuits)
            {
                WaypointCircuit closestCircuit = null;
                var shortestDistance = float.MaxValue;
                foreach (var currentCircuit in circuits)
                {
                    if (!currentCircuit.IsFreeToPatrol()) continue;

                    var closestWaypoint = currentCircuit.GetClosestWaypoint(selfPosition);
                    var currentDistance = closestWaypoint.GlobalPosition.DistanceTo(selfPosition);
                    if (currentDistance > shortestDistance) continue;

                    shortestDistance = currentDistance;
                    closestCircuit = currentCircuit;
                }

                return closestCircuit;
            }

        }
    }
}
