namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using MEC;

public partial class SVOBuilder : Node3D
{
    public float Progress { get; private set; }
    public bool IsDone { get; private set; }
    public SparseVoxelOctree Tree { get; private set; }

    private uint counter;

    public void StartAsyncGeneration(Vector3 worldBoundsCenter, float worldBoundsSize, float minNodeSize)
    {
        Progress = 0f;
        IsDone = false;

        Timing.RunCoroutine(GenerateSVO(worldBoundsCenter, worldBoundsSize, minNodeSize), Segment.PhysicsProcess);
    }

    private IEnumerator<double> GenerateSVO(Vector3 center, float size, float minNodeSize)
    {
        counter = 0;
        
        var root = new VoxelNode(center, size, parent: null);
        root.Children = SubdivideNode(root);
        for (int i = 0; i < root.Children.Length; i++)
        {
            yield return Timing.WaitForOneFrame;
            BuildOctreeRecursive(root.Children[i], minNodeSize);
            Progress = (i + 1) / 8f;
        }

        Tree = new SparseVoxelOctree(root);
        IsDone = true;
        Progress = 1f;
    }

    private void BuildOctreeRecursive(VoxelNode currentNode, float minNodeSize)
    {
        var intersectsObstacle = CheckShapeCollision(currentNode.Position, currentNode.Size);
            // CheckShapeCollisionThreadSafe(spaceRid, currentNode.Position, currentNode.Size);
        if (!intersectsObstacle)
        {
            // Empty leaf; stop subdividing
            currentNode.IsLeaf = true;
            currentNode.IsEmpty = true;
            return;
        }

        if (currentNode.Size <= minNodeSize)
        {
            // Reached minNodeSize; stop subdividing
            currentNode.IsLeaf = true;
            currentNode.IsEmpty = false;
            return;
        }

        currentNode.IsLeaf = false;
        currentNode.Children = SubdivideNode(currentNode);
        foreach (var child in currentNode.Children)
            BuildOctreeRecursive(child, minNodeSize);
    }

    private static bool CheckShapeCollisionThreadSafe(Rid spaceRid, Vector3 position, float size)
    {
        var spaceState = PhysicsServer3D.SpaceGetDirectState(spaceRid);
        if (spaceState == null) return false;
    
        var query = new PhysicsShapeQueryParameters3D();
    
        using var box = new BoxShape3D();
        box.Size = Vector3.One * size;
        query.Shape = box;
        query.Transform = new Transform3D(Basis.Identity, position);
        query.CollisionMask = (1 << 4 - 1) | (1 << 8 - 1); // Buildings(4) and InvisibleBoundaries(8)
    
        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }

    private bool CheckShapeCollision(Vector3 position, float size)
    {
        using var box = new BoxShape3D();
        box.Size = Vector3.One * size;
        
        var query = new PhysicsShapeQueryParameters3D();
        query.Transform = new Transform3D(Basis.Identity, position);
        query.CollisionMask = (1 << 4 - 1) | (1 << 8 - 1);
        query.Shape = box;
        
        var spaceState = GetWorld3D().DirectSpaceState;
        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }

    /// <summary>
    /// Given <c>thisNode</c>, create eight voxel nodes of half size and place each one inside it, filling it up.
    /// </summary>
    /// <param name="thisNode"></param>
    /// <returns></returns>
    private static VoxelNode[] SubdivideNode(VoxelNode thisNode)
    {
        var children = new VoxelNode[8];
        var halfSize = thisNode.Size * 0.5f;
        var quarterSize = thisNode.Size * 0.25f;

        int index = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var childCenter = thisNode.Position + new Vector3(x * quarterSize, y * quarterSize, z * quarterSize);
                    children[index] = new VoxelNode(childCenter, halfSize, parent: thisNode);
                    index++;
                }
            }
        }

        return children;
    }
}
