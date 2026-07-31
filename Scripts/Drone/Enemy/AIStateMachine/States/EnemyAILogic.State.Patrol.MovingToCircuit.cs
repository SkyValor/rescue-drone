namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record MovingToCircuit : Patrol, IGet<Input.StartScanning>, IGet<Input.PhysicsTick>
        {
            public MovingToCircuit()
            {
                this.OnEnter(() =>
                {
                    var enemy = Get<Mover>();
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

            public Transition On(in Input.PhysicsTick input)
            {
                ComputeMovementToWaypoint(input.Delta);
                return ToSelf();
            }

            public Transition On(in Input.StartScanning input) => To<Scanning>();
            
            private void FindPathToClosestWaypoint(Mover enemy, Data data)
            {
                var origin = enemy.GlobalPosition;
                var target = data.CurrentWaypoint.GlobalPosition;
                var pathfinder = Get<IDronePathfindingSVO>();
                var path = pathfinder.FindPath(origin, target);
                if (path.Count > 0)
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
                var shortestDistance = float.MaxValue;
                WaypointCircuit closestCircuit = null;
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
