namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Godot.Collections;

public interface IDronePathfindingSVO : INode
{
    bool FindPath(int fromID, int toID, World3D world3D, float droneRadius, Array<Rid> exclusion = null);
    
    Vector3 NextPoint { get; }
    Vector3 CurrentPoint { get; }
    Vector3 PreviousPoint { get; }
    
    bool HasNextPoint { get; }
    bool HasPreviousPoint { get; }
    Vector3 ToNextPoint { get; }
}

[Meta(typeof(IAutoNode))]
public partial class DronePathfindingSVO : Node, IDronePathfindingSVO
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    [Export] public bool DebugLines { get; private set; }

    public float DroneRadius { get; set; }
    public World3D World3D { get; set; }
    
    private Vector3[] rawPath;
    private Vector3[] stringPulledPath;
    private Array<Rid> exclusion;
    private SparseVoxelOctreeShape svo;
    private ushort currentPathIndex;

    public void OnResolved()
    {
        svo = OctreeGenerator.Tree;
        
        if (svo is null)
            GD.PrintErr("SVO cannot be found for drone pathfinding.");
    }

    public void OnProcess(double delta)
    {
        if (DebugLines && rawPath is not null && stringPulledPath is not null)
        {
            
        }
    }

    public bool FindPath(int fromID, int toID, World3D world3D, float droneRadius, Array<Rid> exclusion)
    {
        rawPath = svo.CreatePath(fromID, toID);
        if (rawPath is null || rawPath.Length == 0) return false;
        
        stringPulledPath = SmoothPath(world3D, droneRadius, exclusion);
        currentPathIndex = 0;
        return true;
    }

    public bool HasNextPoint => stringPulledPath is not null && stringPulledPath.Length > 0 && currentPathIndex < stringPulledPath.Length - 1;

    public bool HasPreviousPoint => stringPulledPath is not null && currentPathIndex > 0;

    public Vector3 NextPoint => stringPulledPath[currentPathIndex + 1];

    public Vector3 CurrentPoint => stringPulledPath[currentPathIndex];

    public Vector3 PreviousPoint => stringPulledPath[currentPathIndex - 1];

    public Vector3 ToNextPoint => stringPulledPath[++currentPathIndex];

    private Vector3[] SmoothPath(World3D world3D, float droneRadius, Array<Rid> exclusion = null)
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
                if (IsPathClear(rawPath[current], rawPath[next], world3D, droneRadius, exclusion))
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
    
    private void DrawPathLines()
    {
        if (rawPath is null || rawPath.Length == 0 ||
            stringPulledPath is null || stringPulledPath.Length == 0)
        {
            return;
        }

        for (int i = 0; i < rawPath.Length - 1; i++)
            DebugDraw3D.DrawLine(rawPath[i], rawPath[i + 1], Colors.Green);
        
        for (int j = 0; j < stringPulledPath.Length - 1; j++)
            DebugDraw3D.DrawLine(stringPulledPath[j], stringPulledPath[j + 1], Colors.Goldenrod);
    }
    
}
