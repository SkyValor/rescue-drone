namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        // TODO: Update this information to include base methods for multiple child states.
        /// <summary>
        /// A superstate that continuously attempts to detect the player drone. In case of detection,
        /// an event is listened and internal data is updated. Child states will use that information.
        /// </summary>
        [Meta]
        public abstract partial record Enabled : State, IGet<Input.PhysicsTick>
        {
            public Enabled()
            {
                this.OnEnter(() => Get<SightSensor>().DroneOnSight += OnPlayerDetected);
                this.OnExit(() => Get<SightSensor>().DroneOnSight -= OnPlayerDetected);
            }
            
            public virtual Transition On(in Input.PhysicsTick input)
            {
                Get<Data>().PlayerDetectedThisFrame = false;
                Get<SightSensor>().DetectDrones(); // It will fire an event if the player is detected
                return ToSelf();
            }

            private void OnPlayerDetected(IFlyingDrone player)
            {
                if (player is not PlayerDrone playerDrone) return;

                var data = Get<Data>();
                data.PlayerDetectedThisFrame = true;
                data.LastPlayerPosition = playerDrone.GlobalPosition;
                Input(new Input.PlayerDetected());
            }

            protected static bool ReachedFinalTarget(Vector3[] path, EnemyAIDrone enemy, EnemyDroneSettings settings)
            {
                var finalTarget = path[^1];
                var distanceToTarget = enemy.GlobalPosition.DistanceTo(finalTarget);
                return distanceToTarget <= settings.CheckpointRadius && enemy.Velocity.Length() <= settings.StoppingSpeed;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="delta"></param>
            /// <param name="onFinalTargetReached"></param>
            protected void OnMovingAlongPath(double delta, Action onFinalTargetReached)
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                
                var targetPosition = data.SVOPath[data.CurrentPathIndex];

                // If we are headed towards the final target of this path
                if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                {
                    var finalTarget = data.SVOPath[^1];
                    var distanceToTarget = enemy.GlobalPosition.DistanceTo(finalTarget);
                    
                    // If we are withing stopping range and velocity is near zero, lock in the stop
                    if (distanceToTarget <= settings.CheckpointRadius && enemy.Velocity.Length() <= settings.StoppingSpeed)
                    {
                        // We have reached the circuit. Move to another waypoint before scanning.
                        data.SVOPath = null;
                        Output(new Output.VelocityComputed(Vector3.Zero));
                        onFinalTargetReached?.Invoke();
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
            protected void OnEnterScan(EnemyDroneSettings.ScanSettings scanSettings)
            {
                var data = Get<Data>();
                data.ScanDirection = GetRandomLookingDirection(scanSettings);
                data.CurrentScanCount = 0;
                data.CurrentScanTime = 0f;
                data.IsScanning = false;
            }

            /// <summary>
            /// Perform the rotation of the enemy drone until it reaches the target scan direction. When reaching it,
            /// the drone holds still for as long as specified in the <c>scanSettings</c>.
            ///
            /// If there is at least another scan rotation to be had, it computes a new scan direction and allows
            /// the process to begin anew; otherwise it changes the state to <see cref="State.MovingToCircuit"/>.
            /// </summary>
            /// <param name="input"></param>
            /// <param name="scanSettings"></param>
            /// <returns></returns>
            protected Transition OnPhysicsTickScan(Input.PhysicsTick input, EnemyDroneSettings.ScanSettings scanSettings)
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();
                var deltaTime = (float) input.Delta;

                if (data.IsScanning)
                {
                    data.CurrentScanTime += deltaTime;
                    if (data.CurrentScanTime < scanSettings.Duration) return ToSelf();

                    if (++data.CurrentScanCount < scanSettings.NumberOfScans)
                    {
                        data.ScanDirection = GetRandomLookingDirection(scanSettings);
                        data.CurrentScanTime = 0f;
                        data.IsScanning = false;
                    }
                    else
                    {
                        return To<MovingToCircuit>();
                    }
                }

                var enemy = Get<EnemyAIDrone>();
                SmoothlyRotate(enemy, data.ScanDirection, settings.TurnSpeed, deltaTime);
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

                var enemy = Get<EnemyAIDrone>();
                var scanDirection = Get<Data>().ScanDirection;
                var frontDirection = -enemy.Basis.Z;
                
                data.IsScanning = frontDirection.IsEqualApprox(scanDirection);
                return ToSelf();
            }
            
            #endregion
            
        }
    }
}
