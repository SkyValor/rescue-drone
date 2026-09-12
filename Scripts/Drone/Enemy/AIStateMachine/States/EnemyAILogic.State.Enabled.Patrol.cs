namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone will move around in a circuit of waypoints to scan the surrounding area
        /// in search of the player drone.
        /// 
        /// This is done by reserving the right for a <see cref="WaypointCircuit"/>
        /// and moving towards its <see cref="Waypoint"/>s. The drone stops at a waypoint and scans
        /// around for some time before moving onto another waypoint.
        /// </summary>
        [Meta]
        public abstract partial record Patrol : Enabled
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
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                var targetPosition = data.SVOPath[data.CurrentPathIndex];
                
                if (enemy.GlobalPosition.DistanceTo(targetPosition) < settings.CheckpointRadius)
                {
                    if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                        Input(new Input.StartScanning());
                    else
                        data.CurrentPathIndex++;
                    
                    return;
                }

                var patrolMaxSpeed = settings.MaxSpeed * settings.MaxSpeedPercentageAtPatrol;
                ComputeMovementAlongPath(enemy, targetPosition, patrolMaxSpeed, (float) delta);
            }
        }
    }
}
