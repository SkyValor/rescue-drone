namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Godot;

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
            public Vector3 PlayerInitialDetectionPosition { get; set; }
            private Vector3 lastKnownPlayerPosition;
            
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                // DrawPlayerDetectionLine(true);
                var playerDrone = Get<Data>().DetectedPlayer;
                if (playerDrone is not null)
                {
                    lastKnownPlayerPosition = playerDrone.GlobalPosition;
                    TrackPlayerByDistance(playerDrone.GlobalPosition);
                }
                else Input(new Input.PlayerLost());
            
                return ToSelf();
            }
            
            // TODO: Make TrackPlayerByDistance have a margin of error when in Stay state.

            private void TrackPlayerByDistance(Vector3 playerPosition)
            {
                var distanceToPlayer = CheckDistanceToPlayer(playerPosition);
                switch (distanceToPlayer)
                {
                    case PlayerDistanceType.CloseEnough:
                        Input(new Input.PlayerDroneCloseEnough());
                        break;
                    case PlayerDistanceType.TooClose:
                        Input(new Input.PlayerDroneTooClose());
                        break;
                    case PlayerDistanceType.TooFar:
                        Input(new Input.PlayerDroneTooFar());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            private Vector3 CalculateDynamicPursuitTarget(Vector3 playerPosition, Vector3 enemyPosition, EnemyDroneSettings settings)
            {
                // 1. Isolate the horizontal plane (X and Z) to prevent direct vertical stacking
                var toPlayer = playerPosition - enemyPosition;
                var horizontalToPlayer = toPlayer with { Y = 0f };
                var horizontalDistance = horizontalToPlayer.Length();
                
                // If the enemy is directly above/below the player (near-zero horizontal distance), 
                // inject a horizontal offset vector to force it to step out to the side.
                if (horizontalDistance < 1.5f)
                {
                    // Pick an arbitrary orthogonal direction on the XZ plane based on enemy position hash
                    var sideStep = new Vector3(-toPlayer.Z, 0f, toPlayer.X).Normalized();
                    if (sideStep == Vector3.Zero) sideStep = Vector3.Forward;

                    playerPosition += sideStep * 2f;
                    toPlayer = playerPosition - enemyPosition;
                    horizontalToPlayer = toPlayer with { Y = 0f };
                }

                // 2. Calculate Sweet Spot and Error Range
                var distanceToSweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var distanceToPlayerXY = horizontalToPlayer.Length();
                var distanceError = distanceToPlayerXY - distanceToSweetSpot;
                
                var weight = Mathf.Clamp(distanceToPlayerXY, distanceToSweetSpot, settings.MaxDistance);
                var i = Mathf.Lerp(0f, settings.MaxSpeed, weight);
                
                // IMPORTANT!!
                // If we are in state Retreat, it should mean that the distanceError is < 0,
                // because the player drone is closer to us than the sweet spot.
                
                // Likewise, if distanceError is > 0, the player is farther away.

                if (distanceError.IsZeroApprox())
                {
                    // We are at the sweet spot away from the player. Stay here.
                    var target = enemyPosition;
                    target = ApplyPreferredHeightOffset(target, enemyPosition, playerPosition, settings.PreferredHeightOffset);
                    return target;
                }
                
                // 3. Determine ideal target position based on error
                // We want to position ourselves exactly at 'sweetSpot' distance away from the player
                var direction = toPlayer.Normalized();
                
                // If error is positive (too far), pull target closer to player. 
                // If error is negative (too close), push target further away from player.
                var idealTarget = playerPosition - direction * distanceToSweetSpot;
                
                // Apply preferred height offset so the enemy drone stays slightly above the player's eye level
                if (enemyPosition.Y > playerPosition.Y)
                    idealTarget.Y = playerPosition.Y + settings.PreferredHeightOffset;
                if (enemyPosition.Y < playerPosition.Y)
                    idealTarget.Y = playerPosition.Y - settings.PreferredHeightOffset;
                
                return idealTarget;
            }

            protected static Vector3 ApplyPreferredHeightOffset(Vector3 idealTarget, Vector3 enemyPosition,
                Vector3 playerPosition, float heightOffset)
            {
                if (enemyPosition.Y > playerPosition.Y)
                    idealTarget.Y = playerPosition.Y + heightOffset;
                if (enemyPosition.Y < playerPosition.Y)
                    idealTarget.Y = playerPosition.Y - heightOffset;

                return idealTarget;
            }

            public Transition On(in Input.PlayerLost input) => To<Fetch>()
                .With(state => ((Fetch) state).LastKnownPlayerPosition = lastKnownPlayerPosition);
            
        }
    }
}
