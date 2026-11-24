namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Ramming : Chase, IGet<Input.TriggerRamRecoil>
        {
            private VoxelNode targetVoxel;
            private float lastFrameDistance = -1f;
            
            public Ramming()
            {
                this.OnEnter(() =>
                {
                    SetRamSpeedSettings();
                    SetupTargetVoxel();
                    RequestRepath();
                    Output(new Output.EnableRamCollision(IsEnabled: true));
                });
                this.OnExit(() =>
                {
                    RevertSpeedSettings();
                    Output(new Output.EnableRamCollision(IsEnabled: false));
                });
            }

            private void SetRamSpeedSettings()
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();
                data.CurrentMaxSpeed = settings.RamMaxSpeed;
                data.CurrentAcceleration = settings.RamAcceleration;
                data.CurrentDashSpeed = Get<EnemyAIDrone>().Velocity.Length();
            }

            private void RevertSpeedSettings()
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();
                data.CurrentMaxSpeed = settings.MaxSpeed;
                data.CurrentAcceleration = settings.Acceleration;
                data.CurrentDashSpeed = 0f;
            }

            private void SetupTargetVoxel()
            {
                var gameRepo = Get<IGameRepo>();
                var player = gameRepo.PlayerDrone.Value;
                var playerVoxel = gameRepo.SVO.Value.GetNodeAtPositionPrecise(player.GlobalPosition);
                if (playerVoxel is null)
                {
                    DebugConsole.PrintWarning("Enemy Ram phase. Player is at inaccessible voxel or outside of bounds.");
                    Input(new Input.ReturnToIdle());
                    return;
                }
                    
                targetVoxel = playerVoxel;
            }
            
            private void RequestRepath()
            {
                var path = GeneratePathway(Get<Data>().EnemySvoNode.Position, targetVoxel.Position);
                if (path.Length == 0)
                {
                    DebugConsole.PrintError("Error in state Ramming. Generated path is empty.");
                    return;
                }
                if (path.Length == 1)
                {
                    DebugConsole.Print("Path has one point. We are currently on the spot.");
                    return;
                }

                var data = Get<Data>();
                data.SvoPath = path;
                data.CurrentPathIndex = path.Length == 1 ? 0 : 1;
                // LastRepathPosition ?
            }

            /// <summary>
            /// Continuously increase target speed to the current max speed, and dash towards the target voxel.
            /// If we pass by the player, jump state to <see cref="State.RamFlyBy"/>.
            /// </summary>
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                var playerPosition = Get<IGameRepo>().PlayerDrone.Value.GlobalPosition;
                var distance = enemyPosition.DistanceTo(playerPosition);
                if (distance >= lastFrameDistance)
                    return To<RamFlyBy>(); // We are no longer dashing towards player

                // Cache this value for comparison in the next frame
                lastFrameDistance = distance;

                var data = Get<Data>();
                var deltaTime = (float) input.Delta;
                UpdateDashSpeed(data, deltaTime);
                DashToPlayer(data, Get<IGameRepo>().PlayerDrone.Value, deltaTime);
                return ToSelf();
            }

            private static void UpdateDashSpeed(Data data, float deltaTime)
            {
                data.CurrentDashSpeed = Mathf.Lerp(data.CurrentDashSpeed, data.CurrentMaxSpeed,
                    data.CurrentAcceleration * deltaTime);
            }

            private void DashToPlayer(Data data, PlayerDrone player, float deltaTime)
            {
                var enemy = Get<EnemyAIDrone>();
                var toTarget = player.GlobalPosition - enemy.GlobalPosition;
                SmoothlyRotate(enemy, toTarget, Get<EnemyDroneSettings>().TurnSpeed, deltaTime);

                // Let's assume we can get to the target voxel (the player's) directly
                var velocity = enemy.Velocity;
                var targetVelocity = toTarget.Normalized() * data.CurrentMaxSpeed;

                velocity = velocity.Lerp(targetVelocity, data.CurrentAcceleration * deltaTime);
                Output(new Output.VelocityComputed(velocity));
            }

            public Transition On(in Input.TriggerRamRecoil input)
            {
                Output(new Output.RammedIntoPlayer());
                Get<Data>().RamDirection = Get<EnemyAIDrone>().Velocity;
                return To<RamRecoil>();
            }

        }
    }
}
