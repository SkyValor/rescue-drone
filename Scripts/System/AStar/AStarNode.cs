namespace RescueDrone;

using System.Collections.Generic;

public class GraphNode
{
    public readonly int Id;
    private static int nextId;
    
    public readonly List<GraphEdge> Edges = [];
    public readonly OctreeNode OctreeNode;

    // Common A* variables.
    public float F; // Total cost
    public float G; // Cost from the start node to the current node
    public float H; // Heuristic cost
    public GraphNode From;

    public GraphNode(OctreeNode octreeNode)
    {
        Id = ++nextId;
        OctreeNode = octreeNode;
    }

    public override bool Equals(object obj) => obj is GraphNode other && Id == other.Id;

    public override int GetHashCode() => Id;
}
