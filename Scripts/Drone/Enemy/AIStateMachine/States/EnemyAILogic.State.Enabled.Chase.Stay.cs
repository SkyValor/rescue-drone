namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone maintains this position while looking at the player drone.
        /// 
        /// If the player drone gets too close, the state is changed to <see cref="State.Retreat"/>.
        /// Otherwise, if the player drone gets farther away, the state changes to <see cref="State.Pursuit"/>.
        /// </summary>
        [Meta]
        public partial record Stay : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneTooFar>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                var playerPosition = Get<Data>().LastPlayerPosition;
                SmoothlyRotate(enemy, playerPosition, settings.TurnSpeed, (float) input.Delta);
                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooClose input) => To<Retreat>();

            public Transition On(in Input.PlayerDroneTooFar input) => To<Pursuit>();
            
        }
    }
}
