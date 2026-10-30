namespace RescueDrone;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.Collections;
using Godot;
using Godot.Collections;

public interface IPathfindSVO
{
    /// <summary>
    /// Generate and return a point path consisting of A* points from the closest empty leaves to start and end.
    /// The droneRadius must be accurate to properly determine if the drone can fit through tight spaces or avoid
    /// colliding with walls.
    /// </summary>
    /// <remarks>This method must be invoked during physics time.</remarks>
    Vector3[] CreatePath(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclusion = null);
    bool IsPathClear(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclude = null);
}

public class VoxelOctreeAStar : IPathfindSVO, IDisposable
{
    private readonly AStar3D AStar;
    private readonly SparseVoxelOctree Tree;
    private readonly Vector3[] OffsetDirections = [Vector3.Right, Vector3.Left, Vector3.Up, Vector3.Down, Vector3.Back, Vector3.Forward];
    
    public VoxelOctreeAStar(SparseVoxelOctree tree)
    {
        Tree = tree;
        Tree.OnNodeSubdivided += OnNodeSubdivided;
        Tree.OnNodePartiallySubdivided += OnNodePartiallySubdivided;
        Tree.OnNodeSubdividedForCollisions += HandleNodeSubdivided;
        AStar = new AStar3D();

        var emptyLeaves = Tree.GetNavigableWaypoints();
        DebugConsole.Print("Empty leaves at start: " + emptyLeaves.Count);
        AddNavigablePoints(emptyLeaves);
        ConnectAdjacentPoints(emptyLeaves);
    }
    
    private void OnNodeSubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        RemoveNodeFromGraph(parentNode);

        var octantList = octants.ToList();
        AddNavigablePoints(octantList);
        ConnectAdjacentPoints(octantList);
    }

    private void OnNodePartiallySubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        RemoveNodeFromGraph(parentNode);
        
        var octantList = octants.Where(octant => octant is not null).ToList();
        AddNavigablePoints(octantList);
        ConnectAdjacentPoints(octantList);
    }
    
    private void RemoveNodeFromGraph(VoxelNode navigablePoint)
    {
        if (!AStar.HasPoint(navigablePoint.Id)) return;
        
        var connections = AStar.GetPointConnections(navigablePoint.Id);
        foreach (var otherId in connections)
            AStar.DisconnectPoints(navigablePoint.Id, otherId);
        
        AStar.RemovePoint(navigablePoint.Id);
    }

    private void AddNodeToGraph(VoxelNode navigable)
    {
        if (!AStar.HasPoint(navigable.Id))
            AStar.AddPoint(navigable.Id, navigable.Position);
    }

    private void ConnectGraphNodes(VoxelNode a, VoxelNode b)
    {
        if (!AStar.ArePointsConnected(a.Id, b.Id))
            AStar.ConnectPoints(a.Id, b.Id);
    }

    private void AddNavigablePoints(List<VoxelNode> navigablePoints)
    {
        foreach (var navigable in navigablePoints)
        {
            if (!AStar.HasPoint(navigable.Id))
                AStar.AddPoint(navigable.Id, navigable.Position);
        }
    }

    private void ConnectAdjacentPoints(List<VoxelNode> navigablePoints)
    {
        foreach (var navigable in navigablePoints)
        {
            if (!AStar.HasPoint(navigable.Id))
            {
                DebugConsole.Print($"Point with ID '{navigable.Id}' does not exist.");
                continue;
            }
            
            foreach (var offset in OffsetDirections)
            {
                var neighborPosition =
                    navigable.Position +
                    offset * navigable.Size * 0.5f +
                    offset * 0.01f;
                var neighborNode = Tree.GetNodeAtPosition(neighborPosition);
                if (neighborNode is null || !neighborNode.IsEmpty) continue;

                if (!AStar.HasPoint(neighborNode.Id))
                {
                    if (navigable.HasAncestor(neighborNode.Id))
                    {
                        // Avoid connecting a navigable point to one of its ancestors, as the latter is no longer a leaf
                        continue;
                    }
                    
                    DebugConsole.PrintWarning($"Navigable with ID '{navigable.Id}' cannot connect with neighbor with ID '{neighborNode.Id}'.'");
                    continue;
                }

                if (AStar.ArePointsConnected(navigable.Id, neighborNode.Id)) continue;
                
                AStar.ConnectPoints(navigable.Id, neighborNode.Id);
            }
        }
    }

    private void HandleNodeSubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        RemoveNodeFromGraph(parentNode);

        foreach (var octant in octants)
        {
            if (octant is null || octant.IsOccupied) continue;
            AddNodeToGraph(octant);

            // Connect octant to its internal siblings (since they share inner faces)
            foreach (var sibling in octants)
            {
                if (sibling is null || sibling == octant || sibling.IsOccupied) continue;
                if (AreNodesAdjacent(octant, sibling))
                    ConnectGraphNodes(octant, sibling);
            }

            var externalNeighbors = GetFaceNeighbors(octant);
            foreach (var neighbor in externalNeighbors)
                ConnectGraphNodes(octant, neighbor);
        }
    }

    /// <summary>
    /// Finds all valid, unoccupied leaf neighbors touching the 6 faces of this voxel node.
    /// </summary>
    private Set<VoxelNode> GetFaceNeighbors(VoxelNode navigable)
    {
        var halfSize = navigable.Size * 0.5f;
        var offsetDistance = halfSize * 0.05f;
        var neighbors = new Set<VoxelNode>();

        foreach (var direction in OffsetDirections)
        {
            var samplePosition = navigable.Position + direction * offsetDistance;
            var neighbor = Tree.GetNodeAtPosition(samplePosition);
            if (neighbor is null || neighbor == navigable || neighbor.IsOccupied || neighbor.NotLeaf) continue;
            neighbors.Add(neighbor);
        }

        return neighbors;
    }
    
    /// <summary>
    /// Check if voxel node A and voxel node B are face-adjacent.
    /// </summary>
    private static bool AreNodesAdjacent(VoxelNode a, VoxelNode b)
    {
        var actualDistance = a.Position.DistanceTo(b.Position);
        var expectedDistance = a.Size * 0.5f + b.Size * 0.5f;
        return Mathf.IsEqualApprox(actualDistance, expectedDistance, 0.01f);
    }

    public Vector3[] CreatePath(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclusion = null)
    {
        var origin = Tree.GetNodeAtPositionPrecise(start);
        var target = Tree.GetNodeAtPositionPrecise(end);

        if (origin is null || target is null)
        {
            DebugConsole.PrintWarning("Cannot create point path because either origin or target is null.");
            return [];
        }
        
        if (!AStar.HasPoint(origin.Id) || !AStar.HasPoint(target.Id))
        {
            DebugConsole.PrintWarning("Cannot create point path because either origin or target are not in the SVO.");
            return [];
        }
        
        var rawPath = AStar.GetPointPath(origin.Id, target.Id);
        return SmoothPath(rawPath, world, droneRadius, exclusion);
    }

    private void DebugPrintOrigin(VoxelNode origin)
    {
        // Try to hit neighbor nodes here to see if one of them SHOULD be a connection
        foreach (var offset in OffsetDirections)
        {
            var neighborPosition =
                origin.Position +
                offset * origin.Size * 0.5f +
                offset * 0.01f;
            var neighborNode = Tree.GetNodeAtPosition(neighborPosition);
            if (neighborNode is null || !neighborNode.IsEmpty) continue;
                
            // We have a voxel node that exists and is not occupied
                
            DebugConsole.Print($"=> Voxel node with ID '{neighborNode.Id}'");
                
            if (origin.HasAncestor(neighborNode.Id))
            {
                // We don't want a hit when the neighbor is an ancestor
                DebugConsole.Print("Ancestor");
            }

            if (origin.HasDescendant(neighborNode.Id))
            {
                // We don't want a hit when the neighbor is a descendant
                DebugConsole.Print("Descendant");
            }
                
            // We have a voxel node that exists, is not occupied, is not ancestor/descendant to Origin
                
            if (neighborNode.IsLeaf)
            {
                DebugConsole.Print("Leaf");
            }
                
            if (!AStar.HasPoint(neighborNode.Id))
            {
                DebugConsole.Print("HasPoint() is false");
            }
        }
    }
    
    /// <summary>
    /// String Pulling algorithm to smoothen out the raw path. It works by checking line-of-sight between
    /// non-consecutive waypoints and eliminating unnecessary corners.
    /// </summary>
    /// <param name="rawPath"></param>
    /// <param name="world"></param>
    /// <param name="droneRadius"></param>
    /// <param name="exclusion"></param>
    /// <returns></returns>
    private Vector3[] SmoothPath(Vector3[] rawPath, World3D world, float droneRadius, Rid[] exclusion = null)
    {
        if (rawPath.Length <= 2) return rawPath;

        var smoothPath = new List<Vector3> { rawPath[0] };
        var current = 0;
        while (current < rawPath.Length - 1)
        {
            var shortcutFound = false;
            
            // Check from the end of the path backwards.
            for (int next = rawPath.Length - 1; next > current + 1; next--)
            {
                // If the drone can safely fly in a straight line between these two points
                if (IsPathClear(rawPath[current], rawPath[next], world, droneRadius, exclusion))
                {
                    smoothPath.Add(rawPath[next]);
                    current = next; // Move our starting point forward
                    shortcutFound = true;
                    break;
                }
            }

            if (shortcutFound) continue;
            
            // FALLBACK: If no shortcuts were clear, we MUST step forward by 1
            // to prevent an infinite loop. We trust AStar3D's original path step.
            current++;
            smoothPath.Add(rawPath[current]);
        }
        
        return smoothPath.ToArray();
    }
    
    public bool IsPathClear(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclude = null)
    {
        var spaceState = world.DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();

        var exclusion = exclude is not null ? new Array<Rid>(exclude) : null;
        
        // Use a sphere cast matching the drone's size to ensure it doesn't clip walls
        var sphere = new SphereShape3D();
        sphere.Radius = droneRadius;
        query.Shape = sphere;
        
        // We make a motion query to determine how much of the path is safe for traversing
        query.Transform = new Transform3D(Basis.Identity, start);
        query.Motion = end - start;
        query.Exclude = exclusion;
        var result = spaceState.CastMotion(query);
        
        // CastMotion returns an array where [0] is the safe fraction (1.0 means completely clear)
        return result[0] >= 1.0f;
    }
    
    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        Tree.OnNodeSubdivided -= OnNodeSubdivided;
        Tree.OnNodePartiallySubdivided -= OnNodePartiallySubdivided;
        Tree.OnNodeSubdividedForCollisions -= HandleNodeSubdivided;
        AStar.Dispose();
    }
    #endregion
    
}
