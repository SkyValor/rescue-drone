namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

// TODO: Have a sphere hitbox around the enemy drone for a second sensor.
// When the player is being detected, allow this second hitbox to also continuously detect it.
// This avoids the bottleneck of the player getting too close and reaching outside of the sight sensor cone of detection.
// It's also standard for enemies to be aware of their close surroundings, especially when alerted.

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
        public partial record Pursuit : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneCloseEnough>,
            IGet<Input.Moved>
        {
            private readonly Color DebugMinDistanceColor = new(Colors.AntiqueWhite, 0.02f);
            private readonly Color DebugMaxDistanceColor = new(Colors.DarkGoldenrod, 0.045f);
            private readonly Color DebugIdealTargetColor = new(Colors.DeepPink, 0.01f);
            private readonly Color DebugPathEndColor = new(Colors.Purple, 0.4f);
            private Vector3? DebugIdealTarget;
            
            public Pursuit()
            {
                OnAttach(() => DebugConsole.Print("OnAttach state Pursuit"));
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var player = data.DetectedPlayer;
                if (player is null)
                {
                    DebugConsole.PrintError("Error in state Pursuit. Detected player is null!");
                    player = Get<IGameRepo>().PlayerDrone.Value;
                    if (player is null)
                    {
                        DebugConsole.PrintWarning("Warning in state Pursuit. GameRepo has no reference to player drone.");
                        return To<Idle>();
                    }
                }
                
                var settings = Get<EnemyDroneSettings>();
                var enemyPosition = enemy.GlobalPosition;
                var playerPosition = player.GlobalPosition;

                DrawMinMaxDistance(enemyPosition, settings.MinDistance, settings.MaxDistance);
                DrawIdealTarget();
                DrawPathEnd(data);

                var playerPastThreshold = playerPosition.DistanceTo(data.LastRepathPosition) >= settings.RepathThreshold;
                if (data.SVOPath is null || playerPastThreshold)
                {
                    DebugConsole.Print("Either the path is null or the player moved a bunch. Recalculate SVO pathway.");
                    var recalculated = RecalculateSVOPathway(data, enemy, player, settings);
                    if (!recalculated) return ToSelf();
                }

                TravelToSweetSpot(enemy, player, settings, input.Delta);
                return ToSelf();
            }

            private void DrawMinMaxDistance(Vector3 position, float minDistance, float maxDistance)
            {
                DebugDraw3D.DrawCylinderAb(
                    a: position + Vector3.Down * 0.25f,
                    b: position + Vector3.Up * 0.25f,
                    radius: minDistance,
                    color: DebugMinDistanceColor);
                DebugDraw3D.DrawCylinderAb(
                    a: position + Vector3.Down * 0.1f,
                    b: position + Vector3.Up * 0.1f,
                    radius: maxDistance,
                    color: DebugMaxDistanceColor);
            }

            private void DrawIdealTarget()
            {
                if (DebugIdealTarget is null) return; 
                DebugDraw3D.DrawSphere((Vector3) DebugIdealTarget, color: DebugIdealTargetColor);
            }

            private void DrawPathEnd(Data data)
            {
                if (data.SVOPath is null || data.SVOPath.Length == 0) return;
                DebugDraw3D.DrawSphere(data.SVOPath[^1], color: DebugPathEndColor);
            }

            private bool RecalculateSVOPathway(Data data, EnemyAIDrone enemy, PlayerDrone player, EnemyDroneSettings settings)
            {
                var idealTarget = CalculateDynamicPursuitTarget();
                var path = GeneratePathway(enemy.GlobalPosition, idealTarget);
                if (path.Length == 0)
                {
                    DebugConsole.PrintError("Error in state Pursuit. Generated path is empty.");
                    return false;
                }
                if (path.Length == 1)
                {
                    DebugConsole.Print("Path has one point. We are currently on the spot.");
                    return false;
                }
                
                // var idealTarget = CalculatePursuitTarget(enemy, player, settings);
                // DebugIdealTarget = idealTarget;
                //
                // var path = GeneratePathway(enemy.GlobalPosition, idealTarget);
                // if (path.Length <= 0)
                // {
                //     DebugConsole.PrintWarning("Warning in state Pursuit. Generated path is empty.");
                //     return false;
                // }
                
                data.SVOPath = path;
                data.CurrentPathIndex = path.Length == 1 ? 0 : 1;
                data.LastRepathPosition = player.GlobalPosition;
                return true;
            }
            
            private void TravelToSweetSpot(EnemyAIDrone enemy, PlayerDrone player, EnemyDroneSettings settings, double delta)
            {
                var enemyPosition = enemy.GlobalPosition;
                var playerPosition = player.GlobalPosition;
                
                // Calculate how far off from our sweet spot we currently are
                var distanceToPlayer = enemyPosition.DistanceTo(playerPosition);
                var distanceToSweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var maxErrorRange = (settings.MaxDistance - settings.MinDistance) * 0.5f;

                // Intensity factor: 0.0 when right at the sweet spot, 1.0 when hitting Min or Max bounds
                var errorAmount = Mathf.Abs(distanceToPlayer - distanceToSweetSpot);
                var intensity = Mathf.Clamp(errorAmount / maxErrorRange, 0f, 1f);

                // Scale acceleration speed proportionally:
                // The further out of bounds it is, the faster it reacts!
                var targetSpeed = Mathf.Lerp(settings.MaxSpeed * 0.3f, settings.MaxSpeed, intensity);

                // Move the enemy drone towards the ideal target, but keep looking at the player
                var toPlayer = (playerPosition - enemy.GlobalPosition) with { Y = 0f };

                OnMovingAlongPath(delta, targetSpeed, false, onFinalTargetReached: () => Input(new Input.PlayerDroneCloseEnough()));
                SmoothlyRotate(enemy, toPlayer, settings.TurnSpeed, (float) delta);
            }

            /// <summary>
            /// From this drone's perspective, calculate the ideal target position to move to in order to be
            /// in an acceptable range of distance to the player.
            /// </summary>
            /// <param name="enemy"></param>
            /// <param name="player"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            private static Vector3 CalculatePursuitTarget(EnemyAIDrone enemy, PlayerDrone player,
                EnemyDroneSettings settings)
            {
                // var enemyPosition = enemy.GlobalPosition;
                // var playerPosition = player.GlobalPosition;
                //
                // var toPlayer = playerPosition - enemyPosition;
                // var distance = toPlayer.Length();

                // If the drone is outside the player range, accelerate to have higher speed and
                // aim for the center of range

                // If the drone is inside the player range and player is traveling away (distance increasing),
                // accelerate slightly towards it

                // If the drone is inside the player range and player is traveling towards us
                // (distance decreasing), back away slightly

                var toEnemy = enemy.GlobalPosition - player.GlobalPosition;
                var currentDistance = toEnemy.Length();

                if (currentDistance < 0.1f)
                {
                    return player.GlobalPosition + new Vector3(0f, 0f, settings.MinDistance);
                }

                var directionFromPlayer = toEnemy.Normalized();
                var targetDistance = Mathf.Clamp(currentDistance, settings.MinDistance, settings.MaxDistance);
                return player.GlobalPosition + directionFromPlayer * targetDistance;

                // var playerPosition = player.GlobalPosition;
                // var toPlayer = playerPosition - enemy.GlobalPosition;
                // var distance = toPlayer.Length();
                // var targetDistance = Mathf.Clamp(distance, settings.MinDistance, settings.MaxDistance);
                // return playerPosition - toPlayer.Normalized() * targetDistance;
            }

            private Vector3 CalculateDynamicPursuitTarget()
            {
                var settings = Get<EnemyDroneSettings>();
                var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                var playerPosition = Get<Data>().DetectedPlayer.GlobalPosition;

                var toPlayer = playerPosition - enemyPosition;
                var toPlayerXZ = toPlayer with { Y = 0f };
                // var horizontalDistance = toPlayerXZ.Length();
                // if (horizontalDistance < 1.5f)
                // {
                //     // If the enemy is directly above or below the player,
                //     // add a horizontal offset to force it to step out of the side
                //     var sideStep = new Vector3(-toPlayer.Z, 0f, toPlayer.X).Normalized();
                //     if (sideStep == Vector3.Zero) sideStep = Vector3.Forward;
                //     
                //     
                // }

                var distanceSweetSpot = settings.MinDistance + settings.MaxDistance * 0.5f;
                // var distancePlayer = toPlayerXZ.Length();

                // The target speed increases as the player gets farther away from the 'sweet spot' and closer to MaxDistance.
                // Overshooting MaxDistance keeps the target speed at MaxSpeed.
                // var percentage = Utils.PercentageCalculate(distanceSweetSpot, settings.MaxDistance, distancePlayer);
                // percentage = Mathf.Clamp(percentage, 0f, 1f);
                // var targetSpeed = Mathf.Lerp(0f, settings.MaxSpeed, percentage);

                var direction = toPlayerXZ.Normalized();
                var idealTarget = playerPosition - direction * distanceSweetSpot;
                idealTarget = ApplyPreferredHeightOffset(idealTarget, enemyPosition, playerPosition, settings.PreferredHeightOffset);
                DebugIdealTarget = idealTarget;
                return idealTarget;
            }

            public Transition On(in Input.Moved input)
            {
                // Check if the enemy drone is at the end of the path
                var data = Get<Data>();
                var stoppingSpeed = Get<EnemyDroneSettings>().StoppingSpeed;
                var enemyVelocity = Get<EnemyAIDrone>().Velocity;
                var isEndOfPath = data.CurrentPathIndex == data.SVOPath.Length - 1;
                
                if (isEndOfPath && enemyVelocity.Length() <= stoppingSpeed)
                {
                    Output(new Output.VelocityComputed(Vector3.Zero));
                    return To<Stay>();
                }

                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooClose input) => To<Retreat>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();
            
        }
    }
}
