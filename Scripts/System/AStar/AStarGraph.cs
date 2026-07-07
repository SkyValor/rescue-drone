namespace RescueDrone;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AStarGraph
{
    private const int MAX_ITERATIONS = 10000;
    
    public readonly Dictionary<OctreeNode, AStarNode> Nodes = new();
    public readonly HashSet<AStarEdge> Edges = [];

    private readonly List<AStarNode> pathList = [];
    
    public int GetPathLength() => pathList.Count;

    public OctreeNode GetPathNode(int index)
    {
        if (pathList is null) return null;

        if (index < 0 || index >= pathList.Count)
        {
            GD.PrintErr($"Index out of bounds. Path length: {pathList.Count}, Index: {index}");
            return null;
        }

        return pathList[index].OctreeNode;
    }

    public bool AStar(OctreeNode startNode, OctreeNode endNode)
    {
        pathList.Clear();
        var start = FindNode(startNode);
        var end = FindNode(endNode);

        if (start is null || end is null)
        {
            GD.PrintErr("Start or End node not found in the graph.");
            return false;
        }

        var openSet = new SortedSet<AStarNode>(new AStarNodeComparer());
        var closedSet = new HashSet<AStarNode>();
        int iterationCount = 0;

        start.G = 0;
        start.H = Heuristic(start, end);
        start.F = start.G + start.H;
        start.From = null;
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            if (++iterationCount > MAX_ITERATIONS)
            {
                GD.PrintErr("A* exceeded maximum iterations.");
                return false;
            }

            var current = openSet.First();
            openSet.Remove(current);

            if (current.Equals(end))
            {
                ReconstructPath(current);
                return true;
            }
            
            closedSet.Add(current);

            foreach (var edge in current.Edges)
            {
                var neighbor = Equals(edge.A, current) ? edge.B : edge.A;
                if (closedSet.Contains(neighbor)) continue;

                var tentativeGScore = current.G + Heuristic(current, neighbor);

                if (tentativeGScore < neighbor.G || !openSet.Contains(neighbor))
                {
                    neighbor.G = tentativeGScore;
                    neighbor.H = Heuristic(neighbor, end);
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.From = current;
                    openSet.Add(neighbor);
                }
            }
        }
        
        return false;
    }
    
    public void AddNode(OctreeNode octreeNode)
    {
        if (!Nodes.ContainsKey(octreeNode))
            Nodes.Add(octreeNode, new AStarNode(octreeNode));
    }

    public void AddEdge(OctreeNode a, OctreeNode b)
    {
        var nodeA = FindNode(a);
        var nodeB = FindNode(b);

        if (nodeA is null || nodeB is null) return;
        
        var edge = new AStarEdge(nodeA, nodeB);
        if (Edges.Add(edge))
        {
            nodeA.Edges.Add(edge);
            nodeB.Edges.Add(edge);
        }
    }

    public void DrawGraph()
    {
        foreach (var edge in Edges)
            DebugDraw3D.DrawLine(edge.A.OctreeNode.Bounds.GetCenter(), edge.B.OctreeNode.Bounds.GetCenter(), Colors.Gray);
        
        foreach (var node in Nodes.Values)
            DebugDraw3D.DrawSphere(node.OctreeNode.Bounds.GetCenter(), 0.2f, Colors.DodgerBlue);
    }

    private void ReconstructPath(AStarNode current)
    {
        while (current is not null)
        {
            pathList.Add(current);
            current = current.From;
        }

        pathList.Reverse();
    }

    private static float Heuristic(AStarNode a, AStarNode b) => 
        (a.OctreeNode.Bounds.GetCenter() - b.OctreeNode.Bounds.GetCenter()).LengthSquared();
    
    private AStarNode FindNode(OctreeNode octreeNode)
    {
        Nodes.TryGetValue(octreeNode, out AStarNode node);
        return node;
    }
    
}
