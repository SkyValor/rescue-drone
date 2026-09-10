namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

// TODO: Create an "enabled" state and make most of states descend from it (except Disabled and Dead)

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone will move around in a circuit of waypoints to scan the surrounding area.
        /// This is done by reserving the right for a <see cref="WaypointCircuit"/>
        /// and moving towards its <see cref="Waypoint"/>s. The drone stops at a waypoint and scans
        /// around for some time before moving onto another waypoint.
        /// </summary>
        [Meta]
        public abstract partial record Patrol : State, IGet<Input.PlayerInSight>
        {
            protected Patrol()
            {
                this.OnEnter(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.DroneOnSight += OnPlayerOnSight;
                });
                
                this.OnExit(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.DroneOnSight -= OnPlayerOnSight;
                    
                    var data = Get<Data>();
                    if (data.CurrentCircuit is null) return;
                    
                    data.CurrentCircuit.RemovePatrolling();
                    data.CurrentCircuit = null;
                    data.CurrentWaypoint = null;
                });
            }
            
            private void OnPlayerOnSight(IFlyingDrone playerDrone)
            {
                if (playerDrone is not Node3D playerAsNode) return;
                
                Get<Data>().LastPlayerPosition = playerAsNode.GlobalPosition;
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
