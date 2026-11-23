namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        public Vector3 VectorForce { get; set; }
        
        private float elapsedTime;
        private VoxelNode targetNode;
        
        [Meta]
        public partial record RamCooldown : Chase
        {
            public RamCooldown()
            {
                this.OnEnter(() =>
                {
                    elapsedTime = 0f;
                    GetVoxelNode();
                });
                this.OnExit(() =>
                {
                    targetNode = null;
                });
            }

            /// <summary>
            /// Get the voxel node to which the enemy will be backing away to after ramming against the target.
            /// </summary>
            private void GetVoxelNode()
            {
                var awayDirection = (VectorForce.Inverse() with { Y = 0f }).Normalized();
                var awayDistance = Get<EnemyDroneSettings>().RamRecoilDistance;
                var preferredPosition = awayDirection * awayDistance;

                var data = Get<Data>();
                var svo = Get<IGameRepo>().SVO.Value;
                var voxelNode = svo.GetNodeAtPositionPrecise(preferredPosition);
                if (voxelNode is null || voxelNode.IsOccupied)
                {
                    // The node at the preferred position is not navigable
                    
                    for (float weight = 0f; weight <= 1f; weight += 0.2f)
                    {
                        var position = preferredPosition.Lerp(data.EnemySvoNode.Position, weight);
                        voxelNode = svo.GetNodeAtPositionPrecise(position);
                        
                        if (voxelNode is null || voxelNode.IsOccupied) continue;

                        targetNode = voxelNode;
                    }
                }
            }
            
            /// <summary>
            /// Backs away without rotation, in the opposite direction of ramming.
            /// The speed interpolates back to zero quickly.
            /// </summary>
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                if (targetNode is null) return ToSelf();

                // Without rotating the drone, move to the designated spot

                elapsedTime += (float) input.Delta;
            }
            
            
        }
    }
}
