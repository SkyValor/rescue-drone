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
            
            public Ramming()
            {
                this.OnEnter(() =>
                {
                    // Increase the MaxSpeed and Acceleration by their ram modifiers
                });
                this.OnExit(() =>
                {
                    // Revert back the MaxSpeed and Acceleration values
                });
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);

                // TODO: Check if conditions to remain in this state are true
                
                var gameRepo = Get<IGameRepo>();
                var player = gameRepo.PlayerDrone.Value;
                var svo = gameRepo.SVO.Value;

                var playerVoxel = svo.GetNodeAtPositionPrecise(player.GlobalPosition);
                if (playerVoxel is null)
                {
                    DebugConsole.PrintWarning("Enemy Ram phase. Player moved outside SVO bounds.");
                    return To<Idle>();
                }
                if (playerVoxel != targetVoxel)
                {
                    targetVoxel = playerVoxel;
                    RequestRepath();
                }
                
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
