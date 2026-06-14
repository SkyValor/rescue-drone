namespace RescueDrone;

using Godot;
using Godot.Collections;

public partial class Mover : CharacterBody3D
{
    [Export] public float Speed { get; private set; } = 5f;
    [Export] public float Acceleration { get; private set; } = 5f;
    [Export] public float Accuracy { get; private set; } = 1f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public float DroneRadius { get; private set; } = 1f;
    [Export] public float TargetRadius { get; private set; } = 2f;
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }

    private Array<Rid> exclusion;
    private IDronePathfinding dronePathfinding;
    private SparseVoxelOctreeShape svo;
    private Vector3[] aStarPath = [];
    private Vector3[] smoothPath = [];
    private int pathIndex;
    
    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        exclusion = [GetRid()];
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
        
        dronePathfinding = new DronePathfinding();
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
        if (smoothPath.Length == 0 || pathIndex >= smoothPath.Length)
        {
            RecalculatePath();
            return;
        }
        
        var targetPosition = smoothPath[pathIndex];
        var toTarget = targetPosition - GlobalPosition;
        var distance = toTarget.Length();
        if (distance < TargetRadius)
        {
            pathIndex++;
            if (pathIndex >= smoothPath.Length) return;
            
            // Recalculate for the new node
            targetPosition = smoothPath[pathIndex];
            toTarget = targetPosition - GlobalPosition;
        }
        
        // Smoothly rotate towards the destination point.
        var deltaTime = (float) delta;
        var direction = toTarget.Normalized();
        if (direction != Vector3.Zero)
        {
            var targetBasis = Basis.LookingAt(direction, Vector3.Up);
            GlobalTransform = GlobalTransform.InterpolateWith(new Transform3D(targetBasis, GlobalPosition), TurnSpeed * deltaTime);
        }

        var targetVelocity = direction * Speed;
        Velocity = Velocity.Lerp(targetVelocity, Acceleration * deltaTime);
        MoveAndSlide();
    }

    private void RecalculatePath()
    {
        GD.Print($"Recalculating path at {Time.GetTicksMsec() * 0.001f}...");
        if (dronePathfinding is null || svo is null)
        {
            GD.Print($"STOP HERE at {Time.GetTicksMsec() * 0.001f}");
            GD.Print($"{dronePathfinding is null} {svo is null}");
            return;
        }
        
        aStarPath = dronePathfinding.GetAStarPath(svo, GetWorld3D(), GlobalPosition, DroneRadius);
        smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, exclusion);
        pathIndex = 0;
    }

    private void DrawAStarPath()
    {
        if (aStarPath.Length == 0) return;
    
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
        if (smoothPath is null || smoothPath.Length == 0) return;
        
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
        if (aStarPath is null || aStarPath.Length == 0 ||
            smoothPath is null || smoothPath.Length == 0)
        {
            GD.Print("STOP HERE");
            return;
        }

        for (int i = 0; i < aStarPath.Length - 1; i++)
            DebugDraw3D.DrawLine(aStarPath[i], aStarPath[i + 1], Colors.Green);
        
        for (int j = 0; j < smoothPath.Length - 1; j++)
            DebugDraw3D.DrawLine(smoothPath[j], smoothPath[j + 1], Colors.Goldenrod);
    }
    
}
