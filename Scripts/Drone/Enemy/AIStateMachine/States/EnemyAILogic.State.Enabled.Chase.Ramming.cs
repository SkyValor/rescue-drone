namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Ramming : Chase
        {
            private VoxelNode targetVoxel;
            private float lastFrameDistance = -1f;
            
            public Ramming()
            {
                this.OnEnter(() =>
                {
                    SetRamSpeedSettings();
                    DisableCollisionWithPlayer();
                    SetupTargetVoxel();
                    RequestRepath();
                });
                this.OnExit(() =>
                {
                    RevertSpeedSettings();
                    ReEnableCollisionWithPlayer();
                });
            }

            private void SetRamSpeedSettings()
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();
                data.CurrentMaxSpeed = settings.RamMaxSpeed;
                data.CurrentAcceleration = settings.RamAcceleration;
            }

            private void RevertSpeedSettings()
            {
                var data = Get<Data>();
                var settings = Get<EnemyDroneSettings>();
                data.CurrentMaxSpeed = settings.MaxSpeed;
                data.CurrentAcceleration = settings.Acceleration;
            }

            private void DisableCollisionWithPlayer()
            {
                var player = Get<IGameRepo>().PlayerDrone.Value;
                var enemy = Get<EnemyAIDrone>();
                enemy.AddCollisionExceptionWith(player);
            }

            private void ReEnableCollisionWithPlayer()
            {
                var player = Get<IGameRepo>().PlayerDrone.Value;
                var enemy = Get<EnemyAIDrone>();
                enemy.RemoveCollisionExceptionWith(player);
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

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                var playerPosition = Get<IGameRepo>().PlayerDrone.Value.GlobalPosition;
                var distance = enemyPosition.DistanceTo(playerPosition);
                if (distance >= lastFrameDistance)
                {
                    // We are no longer dashing towards player
                    // TODO: Transition to RamFlyBy state
                    return ToSelf();
                }

                // Cache this value for comparison in the next frame
                lastFrameDistance = distance;
                
                // TODO: Change this method in order to override the slowing-down at the end.
                OnMovingAlongPath();
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
            
        }
    }
}
