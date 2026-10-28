namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        protected enum PlayerDistanceType { CloseEnough = 0, TooFar = 1, TooClose = -1 }
        
        /// <summary>
        /// A superstate that represents the drone being enabled and alive.
        /// It holds shared methods that are used by multiple child states with common behavior.
        /// </summary>
        [Meta]
        public partial record Enabled : State, IGet<Input.PhysicsTick>
        {
            private const float SCAN_DIRECTION_MAX_DOT_PRODUCT = 0.7f;
            private readonly Color DebugPlayerSighted = new(Colors.Blue, 0.4f);
            private readonly Color DebugPlayerHidden = new(Colors.Red, 0.4f);
            
            private const string PathIsNullOrEmpty = "Path is null or empty.";
            private const string PathIndexOutOfRange = "Path index is out of range.";
            private const string ReachedEndOfPath = "Reached the end of the path.";
            
            private RandomNumberGenerator random;

            public Enabled()
            {
                OnAttach(() =>
                {
                    var sightSensor = Get<SightSensorCone3D>();
                    sightSensor.DroneSighted += OnDroneSighted;
                    sightSensor.DroneHidden += OnDroneHidden;
                    
                    var proximitySensor = Get<ProximitySensor>();
                    proximitySensor.DroneInProximity += OnDroneVicinityIn;
                    proximitySensor.DroneOutProximity += OnDroneVicinityOut;
                });
                OnDetach(() =>
                {
                    var sightSensor = Get<SightSensorCone3D>();
                    sightSensor.DroneSighted -= OnDroneSighted;
                    sightSensor.DroneHidden -= OnDroneHidden;
                    
                    var proximitySensor = Get<ProximitySensor>();
                    proximitySensor.DroneInProximity -= OnDroneVicinityIn;
                    proximitySensor.DroneOutProximity -= OnDroneVicinityOut;
                });
            }

            #region Subscribed events

            private void OnDroneSighted(IFlyingDrone drone)
            {
                if (drone is not PlayerDrone player) return;
                Input(new Input.PlayerDetected(player));
            }

            private void OnDroneHidden(IFlyingDrone drone)
            {
                if (drone is not PlayerDrone player) return;
                
                // The player exited our field of vision.
                // Unless it is in our surroundings, it's lost to us.

                var inVicinity = Get<ProximitySensor>().DronesInProximity.Contains(player);
                if (inVicinity) return;
                
                Input(new Input.PlayerLost());
            }

            private void OnDroneVicinityIn(IFlyingDrone drone)
            {
                if (drone is not PlayerDrone player) return;
                Input(new Input.PlayerDetected(player));
            }

            private void OnDroneVicinityOut(IFlyingDrone drone)
            {
                if (drone is not PlayerDrone player) return;
                
                // The player exited our surroundings.
                // Unless it is in our field of vision, it's lost to us.

                var onSight = Get<SightSensorCone3D>().DronesOnSight.Contains(player);
                if (onSight) return;

                Input(new Input.PlayerLost());
            }
            
            #endregion
            
            public virtual Transition On(in Input.PhysicsTick input) => ToSelf();

            /// <summary>
            /// Draw a line from the enemy drone towards the player, setting the color based on whether
            /// said player is currently detected or not.
            /// </summary>
            /// <param name="isDetected"></param>
            protected void DrawPlayerDetectionLine(bool isDetected)
            {
                var player = Get<IGameRepo>().PlayerDrone.Value;
                if (player is null) return;

                DebugDraw3D.DrawLine(
                    a: Get<EnemyAIDrone>().GlobalPosition,
                    b: player.GlobalPosition,
                    color: isDetected ? DebugPlayerSighted : DebugPlayerHidden);
            }

            /// <summary>
            /// Check the current distance to the player drone's last known position (in global space).
            /// </summary>
            /// <returns></returns>
            protected PlayerDistanceType CheckDistanceToPlayer(Vector3 playerPosition)
            {
                var settings = Get<EnemyDroneSettings>();
                var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                
                var sweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var distance = enemyPosition.DistanceTo(playerPosition);

                var errorAmount = distance - sweetSpot;
                return errorAmount switch
                {
                    > 1f     => PlayerDistanceType.TooFar,
                    < -0.75f => PlayerDistanceType.TooClose,
                    _        => PlayerDistanceType.CloseEnough
                };
            }

            /// <summary>
            /// Return a <see cref="Transition"/> to change the state into one of <see cref="State.Chase"/>
            /// child states. The returned state will be chosen based on the current distance
            /// to the player drone.
            ///
            /// This method should only be called when the player drone is firstly detected during
            /// a state in which it's not being tracked.
            /// </summary>
            /// <param name="playerPosition"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            protected Transition ToChaseBasedOnDistance(Vector3 playerPosition)
            {
                var distanceType = CheckDistanceToPlayer(playerPosition);
                return distanceType switch
                {
                    PlayerDistanceType.CloseEnough => To<Stay>(),
                    PlayerDistanceType.TooClose    => To<Retreat>(),
                    PlayerDistanceType.TooFar      => To<Pursuit>(),
                    _                              => throw new ArgumentOutOfRangeException()
                };
            }

            #region Traversal methods

            /// <summary>
            /// Get the state machine's current pathfinding instance and request to generate
            /// a point pathway from origin to target.
            /// </summary>
            /// <param name="originPosition"></param>
            /// <param name="targetPosition"></param>
            /// <returns></returns>
            protected Vector3[] GeneratePathway(Vector3 originPosition, Vector3 targetPosition)
            {
                var world = Get<World3D>();
                var enemy = Get<EnemyAIDrone>();
                var pathfinder = Get<IPathfindSVO>();
                return pathfinder.CreatePath(originPosition, targetPosition, world, enemy.DroneRadius, [enemy.GetRid()]);
            }

            protected void OnMovingAlongPath(double delta, float targetSpeed, bool includeRotation = true, Action onFinalTargetReached = null)
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
                    DebugConsole.PrintError(PathIsNullOrEmpty);
                    return;
                }

                if (pathIndex >= path.Length)
                {
                    DebugConsole.PrintError(PathIndexOutOfRange);
                    return;
                }

                // If we are headed towards the final target of this path
                if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                {
                    var finalTarget = data.SVOPath[^1];
                    var distanceToTarget = enemy.GlobalPosition.DistanceTo(finalTarget);

                    // If we are within stopping range and velocity is near zero...
                    if (distanceToTarget <= settings.CheckpointRadius && enemy.Velocity.Length() <= settings.StoppingSpeed)
                    {
                        DebugConsole.Print(ReachedEndOfPath);
                        
                        data.SVOPath = null;
                        Output(new Output.VelocityComputed(Vector3.Zero));
                        onFinalTargetReached?.Invoke();
                        return;
                    }
                }

                else if (enemy.GlobalPosition.DistanceTo(path[pathIndex]) < settings.CheckpointRadius)
                {
                    // We are close enough to consider reaching this point and can now move
                    // to the following point. We do this early to anticipate the curve and have a more
                    // realistic behavior.
                    data.CurrentPathIndex++;
                    return;
                }

                ComputeMovementAlongPath(enemy, includeRotation, targetSpeed, (float) delta);
            }

            /// <summary>
            /// Calculate the velocity that the enemy drone needs to have on this physics frame in order to reach the destination.
            /// This method assumes the drone is following a path and will continue to fly through it.
            ///
            /// The enemy drone slows down when nearing curves in the path. The distance to slow down and how much it slows down
            /// both depend on the tightness of the curve. The enemy drone also slows down until a complete stop when nearing
            /// the end of the path.
            /// </summary>
            /// <param name="enemy"></param>
            /// <param name="includeRotation"></param>
            /// <param name="targetSpeed"></param>
            /// <param name="deltaTime"></param>
            private void ComputeMovementAlongPath(EnemyAIDrone enemy, bool includeRotation, float targetSpeed,
                float deltaTime)
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();

                // Get the desired speed at the next point in this pathway and change the current speed
                // depending on the distance to said point.

                var path = data.SVOPath;
                var pathIndex = data.CurrentPathIndex;
                var headingToFinalTarget = pathIndex == path.Length - 1;

                var currentSpeed = enemy.Velocity.Length();
                var desiredSpeed = headingToFinalTarget
                    ? CalculateFinalTargetSpeed(path, enemy, settings, targetSpeed)
                    : CalculateCurveSpeed(path, pathIndex, settings, enemy.GlobalPosition, targetSpeed);
                var speedBleedingFactor = currentSpeed < desiredSpeed ? settings.Acceleration : settings.Deceleration;
                data.CurrentTargetSpeed = Mathf.Lerp(data.CurrentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);

                var targetPosition = path[pathIndex];
                var toTarget = targetPosition - enemy.GlobalPosition;
                var direction = toTarget.Normalized();

                if (includeRotation)
                    SmoothlyRotate(enemy, toTarget with { Y = 0f }, settings.TurnSpeed, deltaTime);

                // Smoothly alter the current velocity towards the target speed and direction,
                // and output the result.

                var velocity = enemy.Velocity;
                var targetVelocity = direction * data.CurrentTargetSpeed;
                var velocityBleedingFactor = velocity.Length() < data.CurrentTargetSpeed
                    ? settings.Acceleration
                    : settings.Deceleration;

                velocity = velocity.Lerp(targetVelocity, velocityBleedingFactor * deltaTime);
                Output(new Output.VelocityComputed(velocity));
            }

            // private void ComputeMovementWithoutRotation(EnemyAIDrone enemy, Vector3 toDirection, float desiredSpeed, float deltaTime)
            // {
            //     var data = Get<Data>();
            //     var settings = Get<EnemyDroneSettings>();
            //
            //     var speedBleedingFactor = desiredSpeed < data.CurrentTargetSpeed ? settings.Acceleration : settings.Deceleration;
            //     data.CurrentTargetSpeed = Mathf.Lerp(data.CurrentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);
            //
            //     var velocity = enemy.Velocity;
            //     var targetVelocity = toDirection.Normalized() * data.CurrentTargetSpeed;
            //     velocity = velocity.Lerp(targetVelocity, settings.Deceleration * deltaTime);
            //     Output(new Output.VelocityComputed(velocity));
            // }
            
            /// <summary>
            /// Compute the target rotation this drone should have when smoothly turning towards <c>toDirection</c>.
            /// </summary>
            /// <param name="enemy"></param>
            /// <param name="toDirection"></param>
            /// <param name="turnSpeed"></param>
            /// <param name="deltaTime"></param>
            protected void SmoothlyRotate(EnemyAIDrone enemy, Vector3 toDirection, float turnSpeed, float deltaTime)
            {
                if (toDirection == Vector3.Zero) return;

                var targetBasis = Basis.LookingAt(toDirection, Vector3.Up);
                var globalTrans = enemy.GlobalTransform;
                var globalPos = enemy.GlobalPosition;
                globalTrans = globalTrans.InterpolateWith(new Transform3D(targetBasis, globalPos), turnSpeed * deltaTime);
                Output(new Output.RotationComputed(globalTrans));
            }
            
            /// <summary>
            /// Calculate the target speed based on the distance to the final target of this pathway.
            /// The distance required to come to a complete stop from velocity <c>v</c> with deceleration <c>a</c> is: <c>d = v^2 / 2a</c>
            /// </summary>
            /// <param name="path"></param>
            /// <param name="enemy"></param>
            /// <param name="settings"></param>
            /// <param name="targetSpeed"></param>
            /// <returns></returns>
            private static float CalculateFinalTargetSpeed(Vector3[] path, EnemyAIDrone enemy, EnemyDroneSettings settings, float targetSpeed)
            {
                var finalTarget = path[^1];
                var distanceToFinalTarget = enemy.GlobalPosition.DistanceTo(finalTarget);
                
                var currentSpeed = enemy.Velocity.Length();
                var requiredBreakingDistance = (currentSpeed * currentSpeed) / (2f * settings.Deceleration) * 1.2f;
                var arrivalZone = Mathf.Max(requiredBreakingDistance, settings.CheckpointRadius * 2f);

                if (distanceToFinalTarget <= arrivalZone)
                {
                    // Smoothly scale speed down from MaxSpeed to zero as distance approaches zero
                    var arrivalFactor = Mathf.Clamp(distanceToFinalTarget / arrivalZone, 0f, 1f);
                    var easeOutFactor = Mathf.SmoothStep(0f, 1f, arrivalFactor);
                    return targetSpeed * easeOutFactor;
                }

                return targetSpeed;
            }

            /// <summary>
            /// Pinpoint the current location of the enemy drone inside the point pathway. If we are approaching a curve,
            /// then adjust the target speed accordingly. The target speed decreases from a combination of distance to said curve
            /// and how tight said curve is. If we are still not near the next curve, returns <c>maxSpeed</c> instead.
            /// </summary>
            /// <param name="path"></param>
            /// <param name="pathIndex"></param>
            /// <param name="settings"></param>
            /// <param name="currentPosition">current global position</param>
            /// <param name="targetSpeed">the maximum allowed speed the drone can reach right now</param>
            /// <returns></returns>
            private static float CalculateCurveSpeed(Vector3[] path, int pathIndex, EnemyDroneSettings settings,
                Vector3 currentPosition, float targetSpeed)
            {
                // If pathIndex is zero, we make pointA to be where we are. And so, the first line (incomingDir) is the
                // vector from where we are towards the actual first path point (path[0]).
                
                var pointA = pathIndex == 0 ? currentPosition : path[pathIndex - 1];
                var pointB = path[pathIndex];
                var pointC = path[pathIndex + 1];

                // To get how tight a curve is, we calculate the dot product from the current direction
                // and next direction. The result is then remapped to a normalized value, in order to 
                // be able to be used as a weight in our lerp function from minimum speed and maximum speed.

                var incomingDir = (pointB - pointA).Normalized();
                var outgoingDir = (pointC - pointB).Normalized();

                var dot = incomingDir.Dot(outgoingDir);
                var turnTightness = Mathf.Remap(dot, -1f, 1f, 0f, 1f);
                var targetSpeedForTurn = Mathf.Lerp(targetSpeed * settings.MinTurnSpeedPercentage, targetSpeed, turnTightness);

                // We now have the target speed the enemy drone should have at the point of transition from one pathway point
                // to another. The actual speed is now dependent on how close we are to that position.

                return CalculateSpeedNearTarget(currentPosition, pointB, targetSpeedForTurn, targetSpeed,
                    settings.BreakingDistance);
            }

            /// <summary>
            /// Calculate the speed as we approach the designated target. The returned value will be interpolated
            /// between <c>speedAtTarget</c> and <c>startingSpeed</c>, depending on how close we are to the target.
            /// </summary>
            /// <param name="current">current global position</param>
            /// <param name="target">target global position</param>
            /// <param name="startingSpeed">which speed is desired when we are on the target</param>
            /// <param name="maxSpeed">which speed is desired before we reach the breaking distance</param>
            /// <param name="breakingDistance">distance from the target at which we start lowering speed</param>
            /// <returns></returns>
            private static float CalculateSpeedNearTarget(Vector3 current, Vector3 target, float startingSpeed, float maxSpeed, float breakingDistance)
            {
                var distanceToDestination = current.DistanceTo(target);
                if (distanceToDestination >= breakingDistance) 
                    return maxSpeed;
            
                var t = Mathf.Clamp(distanceToDestination / breakingDistance, 0f, 1f);
                return Mathf.Lerp(startingSpeed, maxSpeed, t);
            }
            
            #endregion

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
