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
                var settings = Get<Settings>();
                var deltaTime = (float) input.Delta;

                var toPlayer = player.GlobalPosition - enemy.GlobalPosition;
                var retreatDirection = FindSafeRetreatDirection(enemy, player.GlobalPosition, deltaTime);
                if (retreatDirection == Vector3.Zero) return ToSelf();
                
                // Smoothly rotate to face the player while strafing/retreating away.
                var desiredSpeed = settings.MaxSpeed * 0.6f;
                SmoothlyRotate(enemy, toPlayer.Normalized(), settings.TurnSpeed, deltaTime);
                ComputeMovementWithoutRotation(enemy, retreatDirection, desiredSpeed, deltaTime);
                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooFar input) => To<Pursuit>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();

            private Vector3 FindSafeRetreatDirection(EnemyAIDrone enemy, Vector3 playerPosition, float retreatDistance = 3f)
            {
                var pathfinder = Get<IDronePathfindingSVO>();
                var svo = Get<IGameRepo>().SVOctree.Value;
                
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
                        if (IsRetreatPathClear(svo, pathfinder, enemy.GlobalPosition, targetCheckPoint))
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
                    if (IsRetreatPathClear(svo, pathfinder, enemy.GlobalPosition, upTarget))
                        return Vector3.Up;
                }
                
                // FALLBACK 2: If trapped in a corner completely, stand ground or return zero to halt
                return bestDirection;
            }

            private static bool IsRetreatPathClear(SparseVoxelOctreeShape svo, IDronePathfindingSVO pathfinder, Vector3 start, Vector3 end)
            {
                var targetLeaf = svo.GetLeafAtPosition(end);
                if (targetLeaf is null || !targetLeaf.IsEmpty)
                    return false;

                // Double check with a quick physics sphere cast to avoid static geometry
                return pathfinder.IsPathClear(start, end);
            }
        }
    }
}
