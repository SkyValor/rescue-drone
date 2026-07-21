namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Patrol : State
        {
            protected Patrol()
            {
                this.OnExit(() =>
                {
                    var data = Get<Data>();
                    if (data.CurrentCircuit is null) return;
                    
                    data.CurrentCircuit.RemovePatrolling();
                    data.CurrentCircuit = null;
                    data.CurrentWaypoint = null;
                });
            }

            protected void ComputeMovementToWaypoint(double delta)
            {
                var data = Get<Data>();
                var enemy = Get<Mover>();
                var targetPosition = data.SVOPath[data.CurrentPathIndex];
                if (enemy.GlobalPosition.DistanceTo(targetPosition) < 0.35f)
                {
                    if (data.CurrentPathIndex == data.SVOPath.Count - 1)
                        Input(new Input.StartScanning());
                    else
                        data.CurrentPathIndex++;
                    return;
                }

                var settings = Get<Settings>();
                ComputeMovement(enemy, targetPosition, settings.MaxSpeed * 0.25f, (float) delta);
            }
            
            // public virtual Transition On(in Input.PhysicsTick input)
            // {
            //     var enemy = Get<Mover>();
            //     var gameRepo = Get<IGameRepo>();
            //     var circuits = gameRepo.WaypointCircuits.Value;
            //     
            //     float shortestDistance = float.MaxValue;
            //     WaypointCircuit closestCircuit = null;
            //     foreach (var circuit in circuits)
            //     {
            //         if (!circuit.IsFreeToPatrol()) continue;
            //         
            //         var closestWaypoint = circuit.GetClosestWaypoint(enemy.GlobalPosition);
            //         var currentDistance = closestWaypoint.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            //         if (currentDistance > shortestDistance) continue;
            //         
            //         shortestDistance = currentDistance;
            //         closestCircuit = circuit;
            //     }
            //
            //     if (closestCircuit is null) return ToSelf();
            //     
            //     // Make a reservation on this circuit
            //     Get<Data>().CurrentCircuit = closestCircuit;
            //     closestCircuit.SetPatrolling(enemy);
            //     Input(new Input.MoveToCircuit());
            //     return ToSelf();
            // }
        }
    }
}
