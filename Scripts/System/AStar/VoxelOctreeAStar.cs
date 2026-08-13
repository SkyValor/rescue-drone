namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using Godot.Collections;

public interface IPathfindSVO
{
    /// <summary>
    /// Generate and return a point path consisting of A* points from the closest empty leaves to start and end.
    /// The droneRadius must be accurate to properly determine if the drone can fit through tight spaces or avoid
    /// colliding with walls.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="world"></param>
    /// <param name="droneRadius"></param>
    /// <param name="exclusion"></param>
    /// <remarks>This method must be invoked during physics time.</remarks>
    /// <returns></returns>
    Vector3[] CreatePath(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclusion = null);
    bool IsPathClear(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclude = null);
}

public class VoxelOctreeAStar : IPathfindSVO
{
    private readonly SparseVoxelOctree Tree;
    private readonly AStar3D AStar;

    public VoxelOctreeAStar(SparseVoxelOctree tree)
    {
        Tree = tree;
        AStar = new AStar3D();
        AddEmptyLeavesAsPoints();
    }

    private void AddEmptyLeavesAsPoints()
    {
        foreach (var currentLeaf in Tree.EmptyLeaves)
            AStar.AddPoint(currentLeaf.Id, currentLeaf.Position);
    }
    
    public Vector3[] CreatePath(Vector3 start, Vector3 end, World3D world, float droneRadius, Rid[] exclision = null)
    {
        var origin = Tree.GetClosestEmptyLeaf(start);
        var target = Tree.GetClosestEmptyLeaf(end);

        if (origin is null || target is null) return [];
        
        var rawPath = AStar.GetPointPath(origin.Id, target.Id);
        return SmoothPath(rawPath, world, droneRadius, exclision);
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
    
}
