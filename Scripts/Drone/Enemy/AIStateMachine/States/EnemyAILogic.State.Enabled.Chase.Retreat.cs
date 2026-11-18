namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone finds a way to back away from the player drone. This can be done by moving in the opposite direction,
        /// or in another close by direction. As a fallback, the enemy drone may have to simply ascend, but will do no movement
        /// if there are obstacles all around.
        ///
        /// The enemy drone continues to look at the player drone. If said player is far enough, the state changes to
        /// <see cref="State.Stay"/>; likewise, if the player is too far, the state changes to <see cref="State.Pursuit"/>.
        /// </summary>
        [Meta]
        public partial record Retreat : Chase, IGet<Input.PlayerDroneTooFar>, IGet<Input.PlayerDroneCloseEnough>
        {
            public Retreat()
            {
                OnAttach(() => DebugConsole.Print("OnAttach state Retreat"));
            }
            
            private readonly float[] YawOffsets =
            [
                Mathf.DegToRad(0),
                Mathf.DegToRad(30), Mathf.DegToRad(-30),
                Mathf.DegToRad(60), Mathf.DegToRad(-60),
                Mathf.DegToRad(90), Mathf.DegToRad(-90)
            ];

            private readonly float[] PitchOffsets = [ Mathf.DegToRad(0), Mathf.DegToRad(30), Mathf.DegToRad(-30)];

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var world = Get<World3D>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();

                var deltaTime = (float) input.Delta;
                var enemyPosition = enemy.GlobalPosition;
                var playerPosition = Get<Data>().LastPlayerSvoNode.Position;
                var retreatDistance = 1f + deltaTime;

                var retreatDirection = FindSafeRetreatDirection(enemy, playerPosition, world, retreatDistance);
                if (retreatDirection == Vector3.Zero) return ToSelf();
                
                var distanceToPlayer = enemyPosition.DistanceTo(playerPosition);
                var distanceToSweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var maxErrorRange = (settings.MaxDistance - settings.MinDistance) * 0.5f;

                // Intensity factor: 0.0 when right at the sweet spot, 1.0 when hitting Min or Max bounds
                var errorAmount = Mathf.Abs(distanceToPlayer - distanceToSweetSpot);
                var intensity = Mathf.Clamp(errorAmount / maxErrorRange, 0f, 1f);

                // Set the max speed for Retreat to 75% of MaxSpeed
                var targetSpeed = Mathf.Lerp(settings.MaxSpeed * 0.25f, settings.MaxSpeed * 0.75f, intensity);

                // Smoothly rotate to face the player while strafing/retreating away.
                // var desiredSpeed = settings.MaxSpeed * 0.6f;
                var toPlayer = playerPosition - enemyPosition;
                toPlayer = toPlayer with { Y = 0f };

                OnMovingAlongPath(input.Delta, targetSpeed, false);
                SmoothlyRotate(enemy, toPlayer, settings.TurnSpeed, deltaTime);

                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooFar input) => To<Pursuit>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();

            private VoxelNode FindRetreatTargetVoxel()
            {
                var data = Get<Data>();
                var enemyVoxel = data.EnemySvoNode;
                var playerVoxel = data.LastPlayerSvoNode;

                // If the enemy is above or below the player by more than the maximum allowed,
                // then our priority is to ascend/descend to eye-level. Otherwise, we remain at this height.
                
                // TODO:
                // We can only ascend/descend if the environment allows us. So back away first and change
                // height later.
                
                var positionHeight = GetTargetPositionHeight(enemyVoxel, playerVoxel);
                if (positionHeight.IsEqualApprox(enemyVoxel.Position.Y))
                {
                    // We remain in this height while calculating the ideal target.
                }
                else
                {
                    // We need to ascend/descend to another height.
                }
                
                var toEnemyDirection = (enemyVoxel.Position - playerVoxel.Position).Normalized();

                var projection = enemyVoxel.Position with { Y = positionHeight };
                var toProjectionDir = (projection - playerVoxel.Position).Normalized();
                
                // From this point, we need to verify which distance the projection is from the player and
                // move accordingly.
                
                var toPlayer = playerVoxel.Position - enemyVoxel.Position;
                var idealAwayDirection = toPlayer.Inverse().Normalized();
            }

            private float GetTargetPositionHeight(VoxelNode enemyVoxel, VoxelNode playerVoxel)
            {
                var settings = Get<EnemyDroneSettings>();
                var heightDifference = Mathf.Abs(enemyVoxel.Position.Y - playerVoxel.Position.Y);
                if (heightDifference <= settings.PreferredHeightOffset)
                {
                    // The height difference is okay. The target position could be in our Y-axis position.
                    return enemyVoxel.Position.Y;
                }
                    
                // If the height difference is greater than preferred offset, then we need to calculate where
                // we need to be in the Y-axis.
                
                if (playerVoxel.Position.Y > enemyVoxel.Position.Y) 
                {
                    // The player is higher up than we are. We need to ascend to a certain value
                    // in order to be at eye-level to the player.
                    
                    var targetHeight = playerVoxel.Position.Y - settings.PreferredHeightOffset;
                    var ascendAmount = targetHeight - enemyVoxel.Position.Y;

                    return targetHeight;
                }
                else
                {
                    // The player is lower down than we are. We need to descend to a certain value
                    // in order to be at eye-level to the player.
                    
                    var targetHeight = playerVoxel.Position.Y + settings.PreferredHeightOffset;
                    var descendAmount = enemyVoxel.Position.Y - targetHeight;

                    return targetHeight;
                }
            }
            
            /// <summary>
            /// Attempt to find a retreat direction for the drone to avoid the player. Start by investigating
            /// backwards directions and moving towards a 90º angle. If moving horizontally is impossible without
            /// collision, fallback to ascending (Vector3.Up). If that is impossible as well, halt movement (Vector3.Zero).
            /// </summary>
            private Vector3 FindSafeRetreatDirection(EnemyAIDrone enemy, Vector3 playerPosition, World3D world, float retreatDistance = 3f)
            {
                var voxelOctree = Get<IGameRepo>().SVO.Value;
                var idealAwayDirection = (enemy.GlobalPosition - playerPosition).Normalized();
                var bestDirection = Vector3.Zero;
                var bestScore = -1f;

                foreach (var pitch in PitchOffsets)
                {
                    foreach (var yaw in YawOffsets)
                    {
                        // Rotate the ideal direction by the current candidate angles
                        var candidateDirection = idealAwayDirection
                            .Rotated(Vector3.Up, yaw)
                            .Rotated(idealAwayDirection.Cross(Vector3.Up).Normalized(), pitch)
                            .Normalized();
                        
                        // Check if this candidate path is free of physics colliders and inside empty SVO leaves
                        var targetCheckPoint = enemy.GlobalPosition + (candidateDirection * retreatDistance);
                        if (IsRetreatPathClear(enemy.GlobalPosition, targetCheckPoint, world, voxelOctree))
                        {
                            var score = candidateDirection.Dot(idealAwayDirection);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestDirection = candidateDirection;
                            }
                        }
                    }
                }
                
                // FALLBACK 1: If horizontal escape is blocked by walls, attempt to ascent vertically
                if (bestDirection == Vector3.Zero)
                {
                    var upTarget = enemy.GlobalPosition + (Vector3.Up * retreatDistance);
                    if (IsRetreatPathClear(enemy.GlobalPosition, upTarget, world, voxelOctree))
                        return Vector3.Up;
                }
                
                // FALLBACK 2: If trapped in a corner completely, return zero to halt
                return bestDirection;
            }

            /// <summary>
            /// Check whether the end position is an empty voxel leaf in the Sparse Voxel Octree, and if the movement
            /// towards it results in no collisions. This is achieved by means of a CastMotion.
            /// </summary>
            private bool IsRetreatPathClear(Vector3 start, Vector3 end, World3D world, SparseVoxelOctree svo)
            {
                // Check if the end result is a valid empty leaf
                var targetLeaf = svo.GetNodeAtPosition(end);
                if (targetLeaf is null || !targetLeaf.IsEmpty)
                    return false;

                // Double check with a quick physics sphere cast to avoid static geometry
                var enemy = Get<EnemyAIDrone>();
                var query = new PhysicsShapeQueryParameters3D
                {
                    Transform = new Transform3D(Basis.Identity, start),
                    Motion = end - start,
                    Shape = new SphereShape3D { Radius = enemy.DroneRadius },
                    Exclude = [enemy.GetRid()]
                };

                // CastMotion returns an array where [0] is the safe fraction (1.0 means completely clear)
                var spaceState = world.DirectSpaceState;
                var result = spaceState.CastMotion(query);
                return result[0] >= 1.0f;
            }
        }
    }
}
