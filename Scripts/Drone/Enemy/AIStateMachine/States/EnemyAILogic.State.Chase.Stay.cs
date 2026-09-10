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
        public partial record Stay : Chase
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                // Maintain position while looking at player drone.
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().PlayerDrone.Value;
                var settings = Get<EnemyDroneSettings>();
                SmoothlyRotate(enemy, player.GlobalPosition, settings.TurnSpeed, (float) input.Delta);

                var distanceToPlayer = enemy.GlobalPosition.DistanceTo(player.GlobalPosition);
                return distanceToPlayer < settings.MinDistance
                    ? To<Retreat>()
                    : distanceToPlayer > settings.MaxDistance
                        ? To<Pursuit>()
                        : ToSelf();
            }
        }
    }
}
