namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

// TODO: Use SVO and AStar3D to generate a path to the initial waypoint.

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record MovingToCircuit : Patrol, IGet<Input.StartScanning>
        {
            private Waypoint targetWaypoint;

            public MovingToCircuit()
            {
                this.OnEnter(() =>
                {
                    var enemy = Get<Mover>();
                    var data = Get<Data>();
                    if (data.CurrentCircuit is null)
                    {
                        var gameRepo = Get<IGameRepo>();
                        var circuits = gameRepo.WaypointCircuits.Value;
                        var circuit = GetClosestFreeCircuit(enemy.GlobalPosition, circuits);
                        if (circuit is null)
                        {
                            GD.Print("There are no free circuits for enemy drone to patrol.");
                            Input(new Input.ReturnToIdle());
                            return;
                        }

                        circuit.SetPatrolling(enemy);
                        data.CurrentCircuit = circuit;
                    }

                    targetWaypoint = data.CurrentCircuit.GetClosestWaypoint(enemy.GlobalPosition);
                });
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                var enemy = Get<Mover>();
                if (enemy.GlobalPosition.DistanceTo(targetWaypoint.GlobalPosition) < 0.1f)
                {
                    Input(new Input.StartScanning());
                    return ToSelf();
                }
                
                var settings = Get<Settings>();
                ComputeMovement(enemy, targetWaypoint.GlobalPosition, settings.MaxSpeed * 0.25f, (float) input.Delta);
                return ToSelf();
            }

            public Transition On(in Input.StartScanning input) => To<Scanning>();

            // public Transition On(in Input.MoveToWaypoint input)
            // {
            //     var nextWaypoint = TargetCircuit.NextWaypoint();
            //     return To<ToNextWaypoint>().With(state => ((ToNextWaypoint) state).NextWaypoint = nextWaypoint);
            // }
            
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
