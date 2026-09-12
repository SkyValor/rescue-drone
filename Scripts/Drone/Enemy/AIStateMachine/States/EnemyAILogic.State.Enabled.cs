namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// A superstate that continuously listens for the detection of the player drone and sends input
        /// about it.
        /// </summary>
        [Meta]
        public abstract partial record Enabled : State, IGet<Input.PhysicsTick>
        {
            public Enabled()
            {
                this.OnEnter(() => Get<SightSensor>().DroneOnSight += OnPlayerDetected);
                this.OnExit(() => Get<SightSensor>().DroneOnSight -= OnPlayerDetected);
            }
            
            public virtual Transition On(in Input.PhysicsTick input)
            {
                // We set this flag as false for now. If the nested sight sensor detects the player,
                // it will fire an event and we updated it.
                Get<Data>().PlayerDetectedThisFrame = false;
                return ToSelf();
            }

            private void OnPlayerDetected(IFlyingDrone player)
            {
                if (player is not PlayerDrone playerDrone) return;

                var data = Get<Data>();
                data.PlayerDetectedThisFrame = true;
                data.LastPlayerPosition = playerDrone.GlobalPosition;
                Input(new Input.PlayerDetected(playerDrone));
            }
            
        }
    }
}
