namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public partial class Mover : CharacterBody3D
{
    [Export] public float Speed { get; private set; } = 5f;
    [Export] public float Accuracy { get; private set; } = 1f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public float DroneRadius { get; private set; } = 1f;
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    
    private SparseVoxelOctreeShape svo;
    private Vector3[] aStarPath;
    private Vector3[] smoothPath;
    private int pathIndex;
    private World3D world3D;
    
    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        world3D = GetWorld3D();
        CallDeferred(MethodName.InitiateBehavior);
    }

    private void InitiateBehavior()
    {
        svo = OctreeGenerator.Tree;
        if (svo is null)
        {
            GD.PrintErr("SVO cannot be found in call deferred.");
            return;
        }
        
        GetAStarPath();
        SmoothPath();
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        // DrawAStarPath();
        // DrawSmoothPath();
        DrawPathLines();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (svo is null) return;

        if (smoothPath is null || smoothPath.Length == 0 || pathIndex >= smoothPath.Length)
        {
            GD.Print("Recalculating path...");
            GetAStarPath();
            SmoothPath();
            return;
        }
        
        var distanceToVoxel = GlobalPosition.DistanceTo(smoothPath[pathIndex]);
        if (distanceToVoxel < Accuracy)
        {
            pathIndex++;
            return;
        }
        
        // Smoothly rotate towards the destination point.
        var deltaTime = (float) delta;
        var destination = smoothPath[pathIndex];
        var nextTransform = Transform.LookingAt(destination, Vector3.Up);
        GlobalTransform = GlobalTransform.InterpolateWith(nextTransform, TurnSpeed * deltaTime);
        Velocity = -Basis.Z * Speed * deltaTime;
        MoveAndSlide();
    }

    private void GetAStarPath()
    {
        var closestVoxel = svo.FindClosestEmptyLeaf(GlobalPosition);
        int randomLeafID;
        do
        {
            randomLeafID = GD.RandRange(0, svo.EmptyLeavesCount - 1);
            aStarPath = svo.CreatePath(closestVoxel.Id, randomLeafID);

        } while (closestVoxel.Id == randomLeafID || aStarPath.Length == 0);
        pathIndex = 0;
    }

    private void SmoothPath()
    {
        if (aStarPath.Length <= 2) return;

        var path = new List<Vector3> { aStarPath[0] }; // Always keep the starting point.
        var current = 0;
        while (current < aStarPath.Length - 1)
        {
            // Check from the end of the path backwards.
            for (int next = aStarPath.Length - 1; next > current; next--)
            {
                // If the drone can safely fly in a straight line between these two points
                if (IsPathClear(aStarPath[current], aStarPath[next]))
                {
                    path.Add(aStarPath[next]);
                    current = next; // Move our starting point forward
                    break;
                }
            }
        }

        smoothPath = path.ToArray();
    }

    private bool IsPathClear(Vector3 start, Vector3 end)
    {
        var spaceState = world3D.DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D();
        
        // Use a sphere cast matching the drone's size to ensure it doesn't clip walls
        var sphere = new SphereShape3D();
        sphere.Radius = DroneRadius;
        query.Shape = sphere;
        
        // We make a motion query to determine how much of the path is safe for traversing
        query.Transform = new Transform3D(Basis.Identity, start);
        query.Motion = end - start;
        var result = spaceState.CastMotion(query);
        
        // CastMotion returns an array where [0] is the safe fraction (1.0 means completely clear)
        return result[0] >= 1.0f;
    }

    private void DrawAStarPath()
    {
        if (svo is null || aStarPath.Length == 0) return;
    
        DebugDraw3D.DrawSphere(aStarPath[0], 0.7f, Colors.Blue);
        DebugDraw3D.DrawSphere(aStarPath[^1], 0.7f, Colors.Red);
    
        for (int i = 0; i < aStarPath.Length; i++)
        {
            DebugDraw3D.DrawSphere(aStarPath[i], 0.5f, i == pathIndex ? Colors.Gold : Colors.Green);

            if (i == aStarPath.Length - 1) continue;
            
            var start = aStarPath[i];
            var end = aStarPath[i + 1];
            DebugDraw3D.DrawLine(start, end, Colors.Green);
        }
    }

    private void DrawSmoothPath()
    {
        if (svo is null || smoothPath is null || smoothPath.Length == 0) return;
        
        DebugDraw3D.DrawSphere(smoothPath[0], 0.7f, Colors.Blue);
        DebugDraw3D.DrawSphere(smoothPath[^1], 0.7f, Colors.Red);

        for (int i = 0; i < smoothPath.Length; i++)
        {
            DebugDraw3D.DrawSphere(smoothPath[i], 0.5f, i == pathIndex ? Colors.Gold : Colors.Green);
            
            if (i == smoothPath.Length - 1) continue;
            
            var start = smoothPath[i];
            var end = smoothPath[i + 1];
            DebugDraw3D.DrawLine(start, end, Colors.Green);
        }
    }

    private void DrawPathLines()
    {
        if (svo is null ||
            aStarPath is null || aStarPath.Length == 0 ||
            smoothPath is null || smoothPath.Length == 0)
        {
            GD.Print("STOP HERE");
        }

        for (int i = 0; i < aStarPath.Length - 1; i++)
            DebugDraw3D.DrawLine(aStarPath[i], aStarPath[i + 1], Colors.Green);
        
        for (int j = 0; j < smoothPath.Length - 1; j++)
            DebugDraw3D.DrawLine(smoothPath[j], smoothPath[j + 1], Colors.Goldenrod);
    }
    
}
