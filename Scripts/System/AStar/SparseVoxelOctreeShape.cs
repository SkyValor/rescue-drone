namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public class SparseVoxelOctreeShape
{
    public int EmptyLeavesCount => emptyLeaves.Count;
    
    private OctreeNodeShape Root;
    private List<OctreeNodeShape> emptyLeaves = [];
    
    private readonly Vector3 Size;
    private readonly Vector3 Position;
    private readonly AStar3D AStarGraph;
    private readonly bool SubdivisionMax;

    public SparseVoxelOctreeShape(Vector3 position, Vector3 size, float minNodeSize, World3D world3D, bool subMax = false)
    {
        Size = size;
        Position = position;
        AStarGraph = new AStar3D();
        SubdivisionMax = subMax;
        CreateTree(minNodeSize, world3D);
        AddEmptyLeavesAsPoints();
        LinkVoxelGraph();
    }

    public void DrawTree()
    {
        Root.DrawNode();
    }
    
    public OctreeNodeShape FindClosestEmptyLeaf(Vector3 toPosition)
    {
        var id = AStarGraph.GetClosestPoint(toPosition);
        return id > -1 ? emptyLeaves.Find(leaf => leaf.Id == id) : null;
    }

    public Vector3[] CreatePath(int fromID, int toID)
    {
        if (!AStarGraph.HasPoint(fromID) || !AStarGraph.HasPoint(toID))
            return [];
        
        return AStarGraph.GetPointPath(fromID, toID);
    }

    private void CreateTree(float minNodeSize, World3D world3D)
    {
        emptyLeaves.Clear();
        Root = new OctreeNodeShape(Position, Size, minNodeSize, world3D);
        
        if (SubdivisionMax) 
            Root.SubdivideDepth(ref emptyLeaves);
        else 
            Root.Subdivide(ref emptyLeaves);
    }

    private void AddEmptyLeavesAsPoints()
    {
        foreach (var currentLeaf in emptyLeaves)
        {
            AStarGraph.AddPoint(currentLeaf.Id, currentLeaf.Position);
        }
    }

    private void LinkVoxelGraph()
    {
        const float epsilon = 0.01f;
        foreach (var currentLeaf in emptyLeaves)
        {
            // These are world bounds.
            // TODO: Avoid using magic numbers.
            if (currentLeaf.Position.Y is <= 0 or >= 63) continue;
            
            var center = currentLeaf.Position;
            var halfSize = currentLeaf.Size * 0.5f;
            Vector3[] directions =
            [
                new(halfSize.X + epsilon, 0, 0), // East (+X)
                new(0, halfSize.Y + epsilon, 0), // Up (+Y)
                new(0, 0, halfSize.Z + epsilon)  // South (+Z)
            ];
            
            foreach (var offset in directions)
            {
                var targetPosition = center + offset;
                var neighbor = GetEmptyLeafAtPosition(targetPosition);
                if (neighbor is null) continue;
                if (neighbor.IsLeaf)
                    AStarGraph.ConnectPoints(currentLeaf.Id, neighbor.Id);
            }
        }
    }

    private OctreeNodeShape GetEmptyLeafAtPosition(Vector3 position)
    {
        return Root.ContainsPoint(position) ? FindEmptyLeafRecursive(Root, position) : null;
    }

    private static OctreeNodeShape FindEmptyLeafRecursive(OctreeNodeShape currentNode, Vector3 position)
    {
        if (currentNode.IsLeaf)
            return currentNode.IsEmpty ? currentNode : null;

        foreach (var child in currentNode.Children)
        {
            if (child is not null && child.ContainsPoint(position))
                return FindEmptyLeafRecursive(child, position);
        }

        return null;
    }

}
