namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// This superstate continuously checks the distance from the enemy drone and the player drone.
        /// It sends input to this state machine according to the result of said distance. Child nodes will
        /// react to the distance changing.
        ///
        /// When the player drone is not detected, the state is changed to <see cref="State.Fetch"/>. 
        /// </summary>
        [Meta]
        public partial record Chase : Enabled, IGet<Input.PlayerLost>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var data = Get<Data>();
                if (data.PlayerDetectedThisFrame)
                    CheckDistanceToPlayer();
                else
                    Input(new Input.PlayerLost());

                return ToSelf();
            }
            
            private void CheckDistanceToPlayer()
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                    
                var distanceToPlayer = enemy.GlobalPosition.DistanceTo(data.LastPlayerPosition);
                if (distanceToPlayer < settings.MinDistance)
                    Input(new Input.PlayerDroneTooClose());
                else if (distanceToPlayer > settings.MaxDistance)
                    Input(new Input.PlayerDroneTooFar());
                else
                    Input(new Input.PlayerDroneCloseEnough());
            }
            
            public Transition On(in Input.PlayerLost input) => To<Fetch>();
            
        }
    }
}
