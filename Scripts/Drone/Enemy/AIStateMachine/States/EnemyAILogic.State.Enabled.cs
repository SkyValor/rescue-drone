namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// A superstate that represents the drone being enabled and alive. It continuously attempts to detect the player drone.
        /// In case of detection, an event is listened and internal data is updated. Child states will use that information.
        ///
        /// It also holds shared methods that are used by multiple child state with common behavior.
        /// </summary>
        [Meta]
        public partial record Enabled : State, IGet<Input.PhysicsTick>
        {
            private const float SCAN_DIRECTION_MAX_DOT_PRODUCT = 0.7f;
            private readonly Color DebugPlayerSighted = new(Colors.Blue, 0.4f);
            private readonly Color DebugPlayerHidden = new(Colors.Red, 0.4f);
            
            private RandomNumberGenerator random;

            public virtual Transition On(in Input.PhysicsTick input) => ToSelf();

            protected void DrawPlayerDetectionLine(bool isDetected)
            {
                var player = Get<IGameRepo>().PlayerDrone.Value;
                if (player is null) return;

                DebugDraw3D.DrawLine(
                    a: Get<EnemyAIDrone>().GlobalPosition,
                    b: player.GlobalPosition,
                    color: isDetected ? DebugPlayerSighted : DebugPlayerHidden);
            }
            
            protected void OnMovingAlongPath(double delta, Action onFinalTargetReached)
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();

                var path = data.SVOPath;
                var pathIndex = data.CurrentPathIndex;
                
                // TODO: None of these guard clauses are solving the issue. It will repeat next frame.
                // Create an Input.FixPath and have states that generate a path respond to that input.
                // Maybe have a threshold maximum number of retries before doing something else (like fall-backing to Idle state).
                
                if (path is null || path.Length == 0)
                {
                    DebugConsole.PrintError("Path is null or empty.");
                    return;
                }

                if (pathIndex >= path.Length)
                {
                    DebugConsole.PrintError("Path index is out of range.");
                    return;
                }

                var targetPosition = path[pathIndex];

                // If we are headed towards the final target of this path
                if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                {
                    var finalTarget = data.SVOPath[^1];
                    var distanceToTarget = enemy.GlobalPosition.DistanceTo(finalTarget);

                    // If we are within stopping range and velocity is near zero...
                    if (distanceToTarget <= settings.CheckpointRadius && enemy.Velocity.Length() <= settings.StoppingSpeed)
                    {
                        DebugConsole.Print("Reached end of path.");
                        
                        data.SVOPath = null;
                        Output(new Output.VelocityComputed(Vector3.Zero));
                        onFinalTargetReached?.Invoke();
                        return;
                    }
                }

                else if (enemy.GlobalPosition.DistanceTo(targetPosition) < settings.CheckpointRadius)
                {
                    // We are close enough to consider reaching this point and can now move
                    // to the following point. We do this early to anticipate the curve and have a more
                    // realistic behavior.
                    data.CurrentPathIndex++;
                    return;
                }

                ComputeMovementAlongPath(enemy, targetPosition, settings.MaxSpeed, (float) delta);
            }

            #region Scan functions

            /// <summary>
            /// Performs the initialization of a scanning state. Computes a new scan direction and
            /// resets the parameters to default.
            /// </summary>
            /// <param name="scanSettings"></param>
            protected void OnEnterScan(ScanSettings scanSettings)
            {
                var data = Get<Data>();
                var waypointLookDirection = -data.CurrentWaypoint.Basis.Z;
                random ??= new RandomNumberGenerator();
                
                data.ScanDirection = GetRandomScanDirection(waypointLookDirection, scanSettings);
                data.ScanDuration = random.RandfRange(scanSettings.MinDuration, scanSettings.MaxDuration);
                data.TargetNumberOfScans = random.RandiRange(scanSettings.MinNumberOfScans, scanSettings.MaxNumberOfScans);
                data.CurrentScanCount = 0;
                data.CurrentScanTime = 0f;
                data.IsScanning = false;
            }

            /// <summary>
            /// Perform the rotation of the enemy drone until it reaches the target scan direction. When reaching it,
            /// the drone holds still for as long as specified in the <c>scanSettings</c>.
            ///
            /// If there is at least another scan rotation to be had, it computes a new scan direction and allows
            /// the process to begin anew; otherwise it sends the input <see cref="Input.ScanningComplete"/>.
            /// </summary>
            /// <param name="input"></param>
            /// <param name="scanSettings"></param>
            /// <returns></returns>
            protected Transition OnPhysicsTickScan(Input.PhysicsTick input, ScanSettings scanSettings)
            {
                var data = Get<Data>();
                var deltaTime = (float) input.Delta;

                if (data.IsScanning)
                {
                    data.CurrentScanTime += deltaTime;
                    if (data.CurrentScanTime < data.ScanDuration) return ToSelf();

                    if (++data.CurrentScanCount < data.TargetNumberOfScans)
                    {
                        var waypointLookDirection = -data.CurrentWaypoint.Basis.Z;
                        var previousDirection = data.ScanDirection;

                        data.ScanDirection = GetDifferentScanDirection(waypointLookDirection, scanSettings, previousDirection);
                        data.ScanDuration = random.RandfRange(scanSettings.MinDuration, scanSettings.MaxDuration);
                        data.CurrentScanTime = 0f;
                        data.IsScanning = false;
                    }
                    else
                    {
                        Input(new Input.ScanningComplete());
                        return ToSelf();
                    }
                }

                SmoothlyRotate(Get<EnemyAIDrone>(), data.ScanDirection, scanSettings.RotationSpeed, deltaTime);
                return ToSelf();
            }

            /// <summary>
            /// Checks whether the enemy drone's nose is aligned with the current scan direction and updates
            /// the flag that will start the scanning timer.
            /// </summary>
            /// <returns></returns>
            protected Transition OnMovedScan()
            {
                var data = Get<Data>();
                if (data.IsScanning) return ToSelf();

                var targetDirection = Get<Data>().ScanDirection;
                var droneDirection = -Get<EnemyAIDrone>().Basis.Z;
                var dotProduct = droneDirection.Dot(targetDirection);

                data.IsScanning = Utils.IsFloatApprox(dotProduct, 1f, 0.0001f);
                return ToSelf();
            }
            
            /// <summary>
            /// Generate a random direction to be looked at by the drone during scanning phase. This direction will be equals to
            /// the <c>waypointLookDirection</c> rotated in the X and Y axes by a random amount determined within range from the
            /// <c>settings</c>.
            /// </summary>
            /// <param name="waypointLookDirection"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            private Vector3 GetRandomScanDirection(Vector3 waypointLookDirection, ScanSettings settings)
            {
                var halfX = settings.AngleInX / 2f;
                var halfY = settings.AngleInY / 2f;
                
                var rotationX = random.RandfRange(-halfX, halfX);
                var rotationY = random.RandfRange(-halfY, halfY);
                
                return RotateDirectionInXY(waypointLookDirection, rotationX, rotationY);
            }

            /// <summary>
            /// Return a random direction to be looked at by the drone during scanning phase. This method takes an additional parameter
            /// called <c>previousDirection</c> since the new value will have a dot product inferior to
            /// <see cref="SCAN_DIRECTION_MAX_DOT_PRODUCT"/>.
            /// This will ensure the new direction is significantly different from the previous one.
            /// </summary>
            /// <param name="waypointLookDirection"></param>
            /// <param name="settings"></param>
            /// <param name="previousDirection"></param>
            /// <returns></returns>
            private Vector3 GetDifferentScanDirection(Vector3 waypointLookDirection, ScanSettings settings, Vector3 previousDirection)
            {
                Vector3 newDirection;
                float dotProduct;
                do
                {
                    newDirection = GetRandomScanDirection(waypointLookDirection, settings);
                    dotProduct = newDirection.Dot(previousDirection);
                } while (dotProduct > SCAN_DIRECTION_MAX_DOT_PRODUCT); // The new scan direction has to be somewhat far from the previous one
                
                return newDirection;
            }
            
            /// <summary>
            /// From the <c>baseDirection</c>, compute a new direction that is rotated by <c>yawDegrees</c> in the Y-axis
            /// and by <c>pitchDegrees</c> in the X-axis.
            /// </summary>
            /// <param name="baseDirection"></param>
            /// <param name="yawDegrees"></param>
            /// <param name="pitchDegrees"></param>
            /// <returns></returns>
            private static Vector3 RotateDirectionInXY(Vector3 baseDirection, float yawDegrees, float pitchDegrees)
            {
                // We invert the result of DegToRad because positive degrees turn left and down. We want right and up.
                return baseDirection
                    .Rotated(Vector3.Up, -Mathf.DegToRad(yawDegrees))
                    .Rotated(Vector3.Right, -Mathf.DegToRad(pitchDegrees))
                    .Normalized();
            }
            
            #endregion
            
        }
    }
}
