namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The Idle state is responsible for deciding whether the enemy drone changes its state
        /// to <see cref="State.Chase"/> if there is sight of a player drone or <see cref="State.Patrol"/> otherwise.
        /// </summary>
        [Meta]
        public partial record Idle : Enabled, IGet<Input.PlayerDetected>
        {
            private float timeElapsed;

            public Idle()
            {
                OnAttach(() => timeElapsed = 0f);
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                timeElapsed += (float) input.Delta;
                
                // If we are waiting for longer than IdleWaitTime and no sight of player,
                // change to Patrol state.
                var idleTime = Get<EnemyDroneSettings>().IdleWaitTime;
                return timeElapsed >= idleTime ? To<Patrol>() : ToSelf();
            }

            public Transition On(in Input.PlayerDetected input) => To<Chase>();
            
        }
    }
}
