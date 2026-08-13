namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Retreat : Chase, IGet<Input.PlayerDroneTooFar>, IGet<Input.PlayerDroneCloseEnough>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().Player.Value;
                var settings = Get<EnemyDroneSettings>();
                var deltaTime = (float) input.Delta;
                var world = Get<World3D>();

                var toPlayer = player.GlobalPosition - enemy.GlobalPosition;
                var retreatDirection = FindSafeRetreatDirection(enemy, player.GlobalPosition, world, deltaTime);
                if (retreatDirection == Vector3.Zero) return ToSelf();
                
                // Smoothly rotate to face the player while strafing/retreating away.
                var desiredSpeed = settings.MaxSpeed * 0.6f;
                SmoothlyRotate(enemy, toPlayer.Normalized(), settings.TurnSpeed, deltaTime);
                ComputeMovementWithoutRotation(enemy, retreatDirection, desiredSpeed, deltaTime);
                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooFar input) => To<Pursuit>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();

            /// <summary>
            /// Attempt to find a retreat direction for the drone to avoid the player. Start by investigating
            /// backwards directions and moving towards a 90º angle. If moving horizontally is impossible without
            /// collision, fallback to ascending (Vector3.Up). If that is impossible as well, halt movement (Vector3.Zero).
            /// </summary>
            /// <param name="enemy"></param>
            /// <param name="playerPosition"></param>
            /// <param name="world"></param>
            /// <param name="retreatDistance"></param>
            /// <returns></returns>
            private Vector3 FindSafeRetreatDirection(EnemyAIDrone enemy, Vector3 playerPosition, World3D world, float retreatDistance = 3f)
            {
                var voxelOctree = Get<IGameRepo>().SVO.Value;
                var idealAwayDirection = (enemy.GlobalPosition - playerPosition).Normalized();

                // Testing straight back, then 30º left/right, 60º left/right, 90º left/right, and upwards
                var yawOffsets = new[] { 0f, 0.52f, -0.52f, 1.04f, -1.04f, 1.57f, -1.57f };
                var pitchOffsets = new[] { 0f, 0.4f, -0.4f }; // Slight vertical variance

                var bestDirection = Vector3.Zero;
                var bestScore = -1f;

                foreach (var pitch in pitchOffsets)
                {
                    foreach (var yaw in yawOffsets)
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
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <param name="world"></param>
            /// <param name="svo"></param>
            /// <returns></returns>
            private bool IsRetreatPathClear(Vector3 start, Vector3 end, World3D world, SparseVoxelOctree svo)
            {
                // Check if the end result is a valid empty leaf
                var targetLeaf = svo.GetLeafAtPosition(end);
                if (targetLeaf is null || !targetLeaf.IsEmpty)
                    return false;

                // Double check with a quick physics sphere cast to avoid static geometry
                var enemy = Get<EnemyAIDrone>();
                var sphere = new SphereShape3D();
                sphere.Radius = enemy.DroneRadius;

                var query = new PhysicsShapeQueryParameters3D
                {
                    Transform = new Transform3D(Basis.Identity, start),
                    Motion = end - start,
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
