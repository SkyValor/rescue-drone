namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class DronePathfinding : IDronePathfinding
{
    private const int MIN_PATH_LENGTH = 3;
    
    // public Vector3[] GetAStarPath(SparseVoxelOctreeShape svo, World3D world3D, Vector3 initialPosition, float droneRadius)
    // {
    //     GD.Print("\nGenerateAStarPath()");
    //     Vector3[] path;
    //     var disabledPoints = new List<Vector3>();
    //     var closestVoxel = svo.FindClosestEmptyLeaf(initialPosition);
    //     bool walkable;
    //     do
    //     {
    //         // Generate a walkable path with at least 3 points.
    //         int randomLeafID;
    //         do
    //         {
    //             GD.Print("generating new path...");
    //             randomLeafID = GD.RandRange(0, svo.EmptyLeavesCount - 1);
    //             path = svo.CreatePath(closestVoxel.Id, randomLeafID);
    //             GD.Print($"> Path generated with {path.Length} points.");
    //         } while (closestVoxel.Id == randomLeafID || path.Length < MIN_PATH_LENGTH);
    //
    //         // Check if the current path is completely walkable.
    //         walkable = true;
    //         GD.Print("checking walkability...");
    //         for (int i = 0; i < path.Length - 1; i++)
    //         {
    //             var current = path[i];
    //             var next = path[i + 1];
    //             
    //             if (IsPathClear(current, next, world3D, droneRadius)) continue;
    //             
    //             // If not, set the target node as unwalkable
    //             svo.DisablePoint(next);
    //             disabledPoints.Add(next);
    //             walkable = false;
    //             break;
    //         }
    //     } while (walkable);
    //     
    //     GD.Print("Path generated successfully.");
    //     svo.ReenablePoints(disabledPoints);
    //     return path;
    // }
    
    public Vector3[] GetAStarPath(SparseVoxelOctreeShape svo, World3D world3D, Vector3 initialPosition, float droneRadius)
    {
        Vector3[] path;
        var closestVoxel = svo.FindClosestEmptyLeaf(initialPosition);
        // Generate a walkable path with at least 3 points.
        int randomLeafID;
        do
        {
            randomLeafID = GD.RandRange(0, svo.EmptyLeavesCount - 1);
            path = svo.CreatePath(closestVoxel.Id, randomLeafID);
        } while (closestVoxel.Id == randomLeafID || path.Length < MIN_PATH_LENGTH);
        return path;
    }

    public Vector3[] SmoothPath(Vector3[] rawPath, World3D world3D, float droneRadius, Array<Rid> exclusion = null)
    {
        if (rawPath.Length <= 2) return rawPath;

        var smoothPath = new List<Vector3> { rawPath[0] }; // Always keep the starting point.
        var current = 0;
        while (current < rawPath.Length - 1)
        {
            var shortcutFound = false;
            
            // Check from the end of the path backwards.
            for (int next = rawPath.Length - 1; next > current + 1; next--)
            {
                // If the drone can safely fly in a straight line between these two points
                if (IsPathClear(rawPath[current], rawPath[next], world3D, droneRadius))
                {
                    smoothPath.Add(rawPath[next]);
                    current = next; // Move our starting point forward
                    shortcutFound = true;
                    break;
                }
            }
            
            // FALLBACK: If no shortcuts were clear, we MUST step forward by 1
            // to prevent an infinite loop. We trust AStar3D's original path step.
            if (shortcutFound) continue;
            
            current++;
            smoothPath.Add(rawPath[current]);
        }

        return smoothPath.ToArray();
    }
    
    private static bool IsPathClear(Vector3 start, Vector3 end, World3D world3D, float droneRadius, Array<Rid> exclude = null)
    {
        var spaceState = world3D.DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();
        
        // Use a sphere cast matching the drone's size to ensure it doesn't clip walls
        var sphere = new SphereShape3D();
        sphere.Radius = droneRadius;
        query.Shape = sphere;
        
        // We make a motion query to determine how much of the path is safe for traversing
        query.Transform = new Transform3D(Basis.Identity, start);
        query.Motion = end - start;
        query.Exclude = exclude;
        var result = spaceState.CastMotion(query);
        
        // CastMotion returns an array where [0] is the safe fraction (1.0 means completely clear)
        return result[0] >= 1.0f;
    }
    
}
