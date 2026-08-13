namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Pursuit : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneCloseEnough>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().Player.Value;
                var settings = Get<EnemyDroneSettings>();
                
                var idealTarget = CalculatePursuitTarget(enemy, player, settings);
                if (idealTarget.DistanceTo(data.LastPlayerPosition) <= settings.RepathThreshold) 
                    return ToSelf();
                
                data.LastPlayerPosition = idealTarget;

                var path = GeneratePathway(enemy.GlobalPosition, idealTarget);
                if (path.Length <= 0) return ToSelf();
                
                data.SVOPath = path;
                data.CurrentPathIndex = 1;
                return ToSelf();
            }
            
            private static Vector3 CalculatePursuitTarget(EnemyAIDrone enemy, PlayerMover player, EnemyDroneSettings settings)
            {
                var playerPosition = player.GlobalPosition;
                var toPlayer = playerPosition - enemy.GlobalPosition;
                var distance = toPlayer.Length();
                var targetDistance = Mathf.Clamp(distance, settings.MinDistance, settings.MaxDistance);
                return playerPosition - toPlayer.Normalized() * targetDistance;
            }

            public Transition On(in Input.PlayerDroneTooClose input) => To<Retreat>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();
            
        }
    }
}
