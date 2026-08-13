namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Patrol : State, IGet<Input.PlayerInSight>
        {
            protected Patrol()
            {
                this.OnEnter(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.PlayerInSight += OnPlayerOnSight;
                });
                
                this.OnExit(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.PlayerInSight -= OnPlayerOnSight;
                    
                    var data = Get<Data>();
                    if (data.CurrentCircuit is null) return;
                    
                    data.CurrentCircuit.RemovePatrolling();
                    data.CurrentCircuit = null;
                    data.CurrentWaypoint = null;
                });
            }
            
            private void OnPlayerOnSight(Vector3 playerPosition)
            {
                Get<Data>().LastPlayerPosition = playerPosition;
                Input(new Input.PlayerInSight());
            }

            public Transition On(in Input.PlayerInSight input) => To<Pursuit>();

            protected void ComputeMovementToWaypoint(double delta)
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var targetPosition = data.SVOPath[data.CurrentPathIndex];
                if (enemy.GlobalPosition.DistanceTo(targetPosition) < 0.35f)
                {
                    if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                        Input(new Input.StartScanning());
                    else
                        data.CurrentPathIndex++;
                    return;
                }

                var settings = Get<EnemyDroneSettings>();
                ComputeMovementAlongPath(enemy, targetPosition, settings.MaxSpeed * 0.25f, (float) delta);
            }
        }
    }
}
