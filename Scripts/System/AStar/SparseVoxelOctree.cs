namespace RescueDrone;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public class SparseVoxelOctree
{
    public VoxelNode Root { get; }
    public List<VoxelNode> EmptyLeaves { get; } = [];

    public SparseVoxelOctree(VoxelNode root)
    {
        Debug.Assert(root != null, "ERROR: Initiating a Sparse Voxel Octree with null root.");
        
        Root = root;
        FindEmptyLeavesRecursive(Root);
    }
    
    private void FindEmptyLeavesRecursive(VoxelNode currentNode)
    {
        if (currentNode.IsLeaf)
        {
            if (currentNode.IsEmpty)
                EmptyLeaves.Add(currentNode);
            
            return;
        }
        
        foreach (var childNode in currentNode.Children)
            FindEmptyLeavesRecursive(childNode);
    }
    
    public VoxelNode GetClosestEmptyLeaf(Vector3 toPosition)
    {
        VoxelNode closestNode = null;
        float distance = float.MaxValue;
        foreach (var leaf in EmptyLeaves)
        {
            if (leaf.Position.DistanceSquaredTo(toPosition) >= distance)
                continue;
            
            closestNode = leaf;
            distance = leaf.Position.DistanceSquaredTo(toPosition);
        }

        return closestNode;
    }
    
    /// <summary>
    /// Get the leaf voxel node which contains the given position.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public VoxelNode GetLeafAtPosition(Vector3 position)
    {
        return Root.ContainsPoint(position) ? FindLeafRecursive(Root, position) : null;
    }

    private static VoxelNode FindLeafRecursive(VoxelNode currentNode, Vector3 position)
    {
        if (currentNode.IsLeaf) return currentNode;

        return (
                from child in currentNode.Children 
                where child is not null && child.ContainsPoint(position) 
                select FindLeafRecursive(child, position))
            .FirstOrDefault();
    }

    public void DrawVoxelOctree()
    {
        Root.DrawVoxelWireframe();
    }
    
}
