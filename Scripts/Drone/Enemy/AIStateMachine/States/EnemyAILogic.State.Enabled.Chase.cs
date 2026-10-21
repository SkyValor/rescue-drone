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
        public abstract partial record Chase : Enabled, IGet<Input.PlayerLost>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                // DrawPlayerDetectionLine(true);
                if (Get<Data>().DetectedPlayer is not null)
                {
                    var distanceToPlayer = CheckDistanceToPlayer();
                    if (distanceToPlayer < 0) Input(new Input.PlayerDroneTooClose());
                    else if (distanceToPlayer > 0) Input(new Input.PlayerDroneTooFar());
                    else Input(new Input.PlayerDroneCloseEnough());
                }
                else Input(new Input.PlayerLost());
            
                return ToSelf();
            }
            
            public Transition On(in Input.PlayerLost input) => To<Fetch>();
            
        }
    }
}
