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
        ///
        /// In case of player detection, the state is changed to <see cref="State.Pursuit"/>.
        /// </summary>
        [Meta]
        public abstract partial record Patrol : Enabled, IGet<Input.PlayerDetected>
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

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                DrawPlayerDetectionLine(false);
                return ToSelf();
            }

            public Transition On(in Input.PlayerDetected input) => To<Pursuit>();

        }
    }
}
