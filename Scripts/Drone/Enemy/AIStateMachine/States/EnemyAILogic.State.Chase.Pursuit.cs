namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone closes in on the player drone. This is done by getting an ideal target position,
        /// set between an acceptable range of distance, and generating a pathway to that position.
        ///
        /// This state responds to inputs of the player drone being too close, in which it changes to <see cref="State.Retreat"/>,
        /// and being close enough, in which it changes to <see cref="State.Stay"/>.
        /// The first is useful to avoid cases in which the player drone is way too close to the enemy and hinder its sight sensor and weapons.
        /// </summary>
        [Meta]
        public partial record Pursuit : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneCloseEnough>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().PlayerDrone.Value;
                var settings = Get<EnemyDroneSettings>();
                
                var idealTarget = CalculatePursuitTarget(enemy, player, settings);
                if (idealTarget.DistanceTo(data.LastRepathPosition) <= settings.RepathThreshold) 
                    return ToSelf();
                
                // We register that ideal position only after it being somewhat distant from the
                // last registered position, by a threshold to recalculate the SVO path.
                
                data.LastRepathPosition = idealTarget;

                var path = GeneratePathway(enemy.GlobalPosition, idealTarget);
                if (path.Length <= 0) return ToSelf(); // If length is zero, we don't have a valid path
                
                data.SVOPath = path;
                data.CurrentPathIndex = 1; // We skip 0 because that is where we are
                return ToSelf();
            }
            
            /// <summary>
            /// From this drone's perspective, calculate the ideal target position to move to in order to be
            /// between a range of distance to the player.
            /// </summary>
            /// <param name="enemy"></param>
            /// <param name="player"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            private static Vector3 CalculatePursuitTarget(EnemyAIDrone enemy, PlayerDrone player, EnemyDroneSettings settings)
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
