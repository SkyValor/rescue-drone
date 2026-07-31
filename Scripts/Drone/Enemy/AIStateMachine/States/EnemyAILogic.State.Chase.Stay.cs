namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Stay : Chase
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                // Maintain position while looking at player drone.
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().Player.Value;
                var settings = Get<Settings>();
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
