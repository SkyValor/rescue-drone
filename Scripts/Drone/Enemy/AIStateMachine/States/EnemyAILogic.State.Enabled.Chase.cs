namespace RescueDrone;

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
        public partial record Chase : Enabled, IGet<Input.PlayerLost>
        {
            public Vector3 PlayerInitialDetectionPosition { get; set; }
            private PlayerDistanceType playerDistance = PlayerDistanceType.CloseEnough;

            private Vector3 lastPlayerPosition;
            private Vector3 calculatedPlayerVelocity;
            
            private float currentTargetSpeed;
            private float currentAcceleration;

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                // DrawPlayerDetectionLine(true);
                // ----
                var settings = Get<EnemyDroneSettings>();
                var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                var playerPosition = Get<IGameRepo>().PlayerDrone.Value.GlobalPosition;
                
                var targetPosition = GetAggressiveTarget((float) input.Delta);
                
                // Check if we are currently in an aggressive state (Ramming or Interception)
                var currentDistance = enemyPosition.DistanceTo(playerPosition);
                var isAggressive = currentDistance < settings.RammingTriggerDistance || calculatedPlayerVelocity.Length() > 3;
                if (isAggressive)
                {
                    // Override speed limit to max out aggression during impacts and cutoffs
                    currentTargetSpeed = settings.MaxSpeed * 1.15f;
                    currentAcceleration = settings.Acceleration * 1.45f;
                }
                else
                {
                    // Using standard adaptive proportional speed for sweet spot tracking
                    currentTargetSpeed = CalculateAdaptiveSpeed();
                    currentAcceleration = settings.Acceleration;
                }
                
                // playerDistance = SvoDistanceToPlayer();
                // var idealTarget = CalculateDynamicPursuitTarget();
                
                // var playerDrone = Get<Data>().DetectedPlayer;
                // if (playerDrone is not null)
                // {
                //     lastKnownPlayerPosition = playerDrone.GlobalPosition;
                //     TrackPlayerByDistance(playerDrone.GlobalPosition);
                // }
                // else Input(new Input.PlayerLost());

                return ToSelf();
            }

            public Vector3 GetAggressiveTarget(float deltaTime)
            {
                var settings = Get<EnemyDroneSettings>();
                var player = Get<IGameRepo>().PlayerDrone.Value;
                var playerPosition = player.GlobalPosition;

                calculatedPlayerVelocity = (playerPosition - lastPlayerPosition) / deltaTime;
                lastPlayerPosition = playerPosition;

                var enemy = Get<EnemyAIDrone>();
                var enemyPosition = enemy.GlobalPosition;
                var currentDistanceToPlayer = enemyPosition.DistanceTo(playerPosition);
                var playerSpeed = calculatedPlayerVelocity.Length();

                // --- Priority 1: INTERCEPTION (Cutting Off Fly-Bys) ---
                // If player is moving too fast and heading generally toward or across our sector
                if (playerSpeed > 3f)
                {
                    var toEnemy = (enemyPosition - playerPosition).Normalized();
                    var closingDot = calculatedPlayerVelocity.Normalized().Dot(toEnemy);
                    
                    // If the player's trajectory brings them close to us, cut them off!
                    if (closingDot > -0.2f) // Not flying directly away
                    {
                        // Predict where the player will be in the near future and
                        // target slightly ahead of their path
                        return playerPosition + calculatedPlayerVelocity * settings.InterceptionPredictionTime;
                    }
                }
                
                // --- Priority 2: RAMMING / IMPACT ATTACK (Stacking Distance) ---
                // If the player is too close or trying to close in, ram them!
                if (currentDistanceToPlayer < settings.RammingTriggerDistance)
                {
                    // If we are below the player, charge up into them.
                    // Otherwise, dive-bomb down.
                    var ramTarget = playerPosition;
                    
                    if (enemyPosition.Y < playerPosition.Y) 
                        ramTarget.Y -= 0.5f;
                    else 
                        ramTarget.Y += 0.5f;

                    return ramTarget;
                }
                
                // --- Priority 3: SWEET SPOT PURSUIT (Default Spacing) ---
                // Player is maintaining distance of fleeing. Fall back to the sweet spot orbit.
                var sweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var toPlayer = playerPosition - enemyPosition;

                var horizontalToPlayer = toPlayer with { Y = 0f };
                if (horizontalToPlayer.Length() < 1.5f)
                {
                    
                }

                var toPlayerDirection = toPlayer.Normalized();
                var idealTarget = playerPosition - toPlayerDirection * sweetSpot;
                return idealTarget with { Y = playerPosition.Y + 2f }; // Hover slightly above eye level
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
                        throw new System.ArgumentOutOfRangeException();
                }
            }
            
            // We can only ascend/descend if the horizontal distance to the player is greater than
            // the stacking distance.
            
            
            // If horizontal distance is less than the stacking distance
            
            // 1. We have to move in the opposite direction of its movement.
            
            // 2. If we are above/bellow the player (more than preferred height),
            // then we move towards the preferred height.
            
            // If we are to Retreat (move away from the player), check if the movement is valid (check collisions)
            // If we are ascending/descending, take that into consideration as well. If the ascension/descension
            // is not possible, simply move away from the player until it is.
            
            private Vector3 CalculateDynamicPursuitTarget()
            {
                var data = Get<Data>();
                var enemyVoxel = data.EnemySvoNode;
                var playerVoxel = data.LastPlayerSvoNode;
                var toPlayer = playerVoxel.Position - enemyVoxel.Position;
                var toPlayerDistance = toPlayer.Length();
                
                // TODO:
                // If we are at 'Stay' and the distance to player is between MinDistance and MaxDistance,
                // no need to change. But the enemy needs to ascend/descend to be at eye-level.

                // TODO:
                // 'Chase' base class calculates distance to player and repath distance and calls virtual
                // method RepathRequest(). Derived states override to dictate how to proceed.
                
                var svo = Get<IGameRepo>().SVO.Value;
                var settings = Get<EnemyDroneSettings>();
                var sweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;

                var distanceError = sweetSpot - toPlayerDistance;
                if (distanceError > 0)
                {
                    // The distance to sweet spot is greater than the distance to player. Because the player
                    // is closer to us, we need to back away.

                    // TODO: Input to Retreat and encapsulate this logic there.
                    
                    var retreatVoxel = FindRetreatVoxel(toPlayer.Inverse());
                    if (retreatVoxel is not null) 
                    {
                        // Set this voxel node to be the target position and move the enemy drone there.
                    }
                }

                if (distanceError < 0)
                {
                    // The distance to sweet spot is less than the distance to player. Because the player
                    // is farther to us, we need to pursuit it.
                    
                    // TODO: Input to Pursuit and encapsulate this logic there.

                    var pursuitVoxel = FindDynamicVoxel();
                    if (pursuitVoxel is not null)
                    {
                        // Set this voxel node to be the target position and move the enemy drone there.
                    }
                }
            }

            private VoxelNode FindRetreatVoxel(Vector3 idealAwayDirection)
            {
                var bestDirection = Vector3.Zero;
                var bestScore = -1f;
                
                float[] PitchOffsets = [Mathf.DegToRad(0), Mathf.DegToRad(30), Mathf.DegToRad(-30)];
                float[] YawOffsets =
                [
                    Mathf.DegToRad(0),
                    Mathf.DegToRad(30), Mathf.DegToRad(-30),
                    Mathf.DegToRad(60), Mathf.DegToRad(-60),
                    Mathf.DegToRad(90), Mathf.DegToRad(-90)
                ];

                foreach (var pitch in PitchOffsets)
                {
                    foreach (var yaw in YawOffsets)
                    {
                        var candidateDirection = idealAwayDirection
                            .Rotated(Vector3.Up, yaw)
                            .Rotated(idealAwayDirection.Cross(Vector3.Up).Normalized(), pitch)
                            .Normalized();
                    }
                }
            }

            private static int HeightDifference(Vector3 enemySvoPosition, Vector3 playerSvoPosition)
            {
                if (enemySvoPosition.IsEqualApprox(playerSvoPosition)) return 0;
                if (enemySvoPosition < playerSvoPosition) return -1;
                return 1;
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
