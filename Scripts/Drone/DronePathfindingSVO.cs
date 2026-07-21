namespace RescueDrone;

using Godot;
using Godot.Collections;

public interface IDronePathfindingSVO
{
    /// <summary>
    /// Use the internal Sparse Voxel Octree to generate a path from origin to end with the IDs specified.
    /// Said path will be generated with Godot's AStar3D system, and will return the most optimal one.
    /// This method will be using Voxel String Pulling to make the final path more realistic.
    /// </summary>
    /// <param name="fromID">ID of SVO node to start the path</param>
    /// <param name="toID">ID of SVO node to end the path</param>
    /// <param name="world3D"></param>
    /// <param name="droneRadius"></param>
    /// <param name="exclusion"></param>
    /// <returns></returns>
    bool FindPath(int fromID, int toID, World3D world3D, float droneRadius, Array<Rid> exclusion = null);
    
    Array<Vector3> FindPath(Vector3 originPosition, Vector3 targetPosition, Array<Rid> exclusion = null);
    
    /// <summary>
    /// Returns the next position in this path, if applicable, and without advancing the current index.
    /// </summary>
    Vector3? NextPoint { get; }
    
    /// <summary>
    /// Returns the current position in this path, if applicable.
    /// </summary>
    Vector3 CurrentPoint { get; }
    
    /// <summary>
    /// Returns the previous position in this path, if applicable.
    /// </summary>
    Vector3? PreviousPoint { get; }

    /// <summary>
    /// Returns 
    /// </summary>
    bool HasNextPoint();
    bool HasPreviousPoint();
    Vector3? ToNextPoint { get; }
}

public class DronePathfindingSVO : IDronePathfindingSVO
{
    public float DroneRadius { get; set; }
    public World3D World3D { get; set; }
    private Array<Rid> Exclusion { get; set; }
    
    private Array<Vector3> rawPath;
    private Array<Vector3> path;
    private ushort index;
    private readonly SparseVoxelOctreeShape svo;
    private readonly OctreeGeneratorGroup octreeGenerator;

    public DronePathfindingSVO(OctreeGeneratorGroup octreeGenerator, float droneRadius, World3D world3D)
    {
        this.octreeGenerator = octreeGenerator;
        svo = octreeGenerator.Tree;
        DroneRadius = droneRadius;
        World3D = world3D;

        if (svo is null) GD.PrintErr("SVO cannot be found for drone pathfinding.");
    }

    public bool FindPath(int fromID, int toID, World3D world3D, float droneRadius, Array<Rid> exclusion)
    {
        rawPath = ConvertArray(svo.CreatePath(fromID, toID));
        if (rawPath is null || rawPath.Count == 0) return false;
        
        path = SmoothPath(exclusion);
        index = 0;
        return true;
    }

    public Array<Vector3> FindPath(Vector3 originPosition, Vector3 targetPosition, Array<Rid> exclusion = null)
    {
        var originNode = svo.FindClosestEmptyLeaf(originPosition);
        var targetNode = svo.FindClosestEmptyLeaf(targetPosition);
        if (originNode is null || targetNode is null) return [];
        
        var svoPath = svo.CreatePath(originNode.Id, targetNode.Id);
        if (svoPath.Length == 0) return [];
        
        rawPath = ConvertArray(svoPath);
        path = SmoothPath(exclusion);
        index = 0;
        return path;
    }

    public bool HasNextPoint() => path is not null && index < path.Count - 1;
    public bool HasPreviousPoint() => path is not null && index > 0;

    public Vector3? ToNextPoint => HasNextPoint() ? path[++index] : null;
    public Vector3? NextPoint => HasNextPoint() ? path[index + 1] : null;
    public Vector3 CurrentPoint => path[index];
    public Vector3? PreviousPoint => HasPreviousPoint() ? path[index - 1] : null;

    private Array<Vector3> SmoothPath(Array<Rid> exclusion = null)
    {
        if (rawPath.Count <= 2) return rawPath;

        var smoothPath = new Array<Vector3> { rawPath[0] }; // Always keep the starting point.
        var current = 0;
        while (current < rawPath.Count - 1)
        {
            var shortcutFound = false;
            
            // Check from the end of the path backwards.
            for (int next = rawPath.Count - 1; next > current + 1; next--)
            {
                // If the drone can safely fly in a straight line between these two points
                if (IsPathClear(rawPath[current], rawPath[next], exclusion))
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

        return smoothPath;
    }
    
    private bool IsPathClear(Vector3 start, Vector3 end, Array<Rid> exclude = null)
    {
        var spaceState = World3D.DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();
        
        // Use a sphere cast matching the drone's size to ensure it doesn't clip walls
        var sphere = new SphereShape3D();
        sphere.Radius = DroneRadius;
        query.Shape = sphere;
        
        // We make a motion query to determine how much of the path is safe for traversing
        query.Transform = new Transform3D(Basis.Identity, start);
        query.Motion = end - start;
        query.Exclude = exclude;
        var result = spaceState.CastMotion(query);
        
        // CastMotion returns an array where [0] is the safe fraction (1.0 means completely clear)
        return result[0] >= 1.0f;
    }

    private static Array<Vector3> ConvertArray(Vector3[] svoPath)
    {
        var array = new Array<Vector3>();
        for (int i = 0; i < svoPath.Length; i++)
        {
            array[i] = svoPath[i];
        }
        return array;
    }
    
    private void DrawPathLines()
    {
        if (rawPath is null || rawPath.Count == 0 ||
            path is null || path.Count == 0)
        {
            return;
        }

        for (int i = 0; i < rawPath.Count - 1; i++)
            DebugDraw3D.DrawLine(rawPath[i], rawPath[i + 1], Colors.Green);
        
        for (int j = 0; j < path.Count - 1; j++)
            DebugDraw3D.DrawLine(path[j], path[j + 1], Colors.Goldenrod);
    }
    
}
