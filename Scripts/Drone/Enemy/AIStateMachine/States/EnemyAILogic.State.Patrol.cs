namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Patrol : State, IGet<Input.PhysicsTick>, IGet<Input.MoveToCircuit>
        {
            public virtual Transition On(in Input.PhysicsTick input)
            {
                var enemy = Get<Mover>();
                var gameRepo = Get<IGameRepo>();
                var circuits = gameRepo.WaypointCircuits.Value;
                
                float shortestDistance = float.MaxValue;
                WaypointCircuit closestCircuit = null;
                foreach (var circuit in circuits)
                {
                    if (!circuit.IsFreeToPatrol()) continue;
                    
                    var closestWaypoint = circuit.GetClosestWaypoint(enemy.GlobalPosition);
                    var currentDistance = closestWaypoint.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
                    if (currentDistance > shortestDistance) continue;
                    
                    shortestDistance = currentDistance;
                    closestCircuit = circuit;
                }

                if (closestCircuit is not null)
                {
                    // Make a reservation on this circuit
                    Get<Data>().CurrentCircuit = closestCircuit;
                    closestCircuit.SetPatrolling(enemy);
                    Input(new Input.MoveToCircuit());
                }

                return ToSelf();
            }

            public Transition On(in Input.MoveToCircuit input) => To<MovingToCircuit>();
            {
                var circuit = input.Circuit;
                return To<MovingToCircuit>().With(state => ((MovingToCircuit) state).TargetCircuit = circuit);
            }
        }
    }
}
