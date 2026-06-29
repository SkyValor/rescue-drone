namespace RescueDrone;

using Godot;
using Godot.Collections;

public partial class Mover : CharacterBody3D
{
    // [Export] public float Speed { get; private set; } = 5f;
    [Export] public float MaxSpeed { get; private set; } = 15f;
    [Export] public float Acceleration { get; private set; } = 5f;
    [Export] public float Deceleration { get; private set; } = 8f;
    [Export] public float Accuracy { get; private set; } = 1f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public float DroneRadius { get; private set; } = 1f;
    [Export] public float TargetRadius { get; private set; } = 2f;
    [Export] public float MinTurnSpeedPercentage { get; private set; } = 0.25f;
    [Export] public float BreakingDistance { get; private set; } = 6f;
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    
    // DEBUGGING
    [Export] public Label SpeedLabel { get; private set; }
    [Export] public Label DotLabel { get; private set; }
    [Export] public Label TargetLabel { get; private set; }

    private Array<Rid> exclusion;
    private IDronePathfinding dronePathfinding;
    private SparseVoxelOctreeShape svo;
    private Vector3[] aStarPath = [];
    private Vector3[] smoothPath = [];
    private int currentPathIndex;
    private float currentTargetSpeed;
    
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
        if (SpeedLabel is not null)
            SpeedLabel.Text = $"{currentTargetSpeed}";
        
        // DrawAStarPath();
        // DrawSmoothPath();
        DrawPathLines();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (svo is null) return;
        if (smoothPath.Length == 0 || currentPathIndex >= smoothPath.Length)
        {
            RecalculatePath();
            return;
        }
        
        var targetPosition = smoothPath[currentPathIndex];
        var toTarget = targetPosition - GlobalPosition;
        var distance = toTarget.Length();
        if (distance < TargetRadius)
        {
            currentPathIndex++;
            if (currentPathIndex >= smoothPath.Length) return;
            
            // Recalculate for the new node
            targetPosition = smoothPath[currentPathIndex];
            toTarget = targetPosition - GlobalPosition;
        }

        var deltaTime = (float) delta;
        var desiredSpeed = CalculateAdaptiveSpeed();
        TargetLabel.Text = $"{desiredSpeed}";
        var speedBleedingFactor = desiredSpeed < currentTargetSpeed ? Deceleration : Acceleration;
        currentTargetSpeed = Mathf.Lerp(currentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);
        
        var direction = toTarget.Normalized();
        SmoothlyRotate(direction, deltaTime);

        var targetVelocity = direction * currentTargetSpeed;
        Velocity = Velocity.Lerp(targetVelocity, Acceleration * deltaTime);
        MoveAndSlide();
    }

    private void RecalculatePath()
    {
        if (dronePathfinding is null) return;
        
        aStarPath = dronePathfinding.GetAStarPath(svo, GetWorld3D(), GlobalPosition, DroneRadius);
        smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, exclusion);
        currentPathIndex = 0;
        currentTargetSpeed = MaxSpeed;
    }

    private void SmoothlyRotate(Vector3 toDirection, float deltaTime)
    {
        if (toDirection == Vector3.Zero) return;
        
        var targetBasis = Basis.LookingAt(toDirection, Vector3.Up);
        GlobalTransform = GlobalTransform.InterpolateWith(new Transform3D(targetBasis, GlobalPosition), TurnSpeed * deltaTime);
    }

    // Dynamic speed scaling based on curvature
    private float CalculateAdaptiveSpeed()
    {
        if (smoothPath == null || currentPathIndex == 0 || currentPathIndex == smoothPath.Length - 1)
            return MaxSpeed;
        
        var pointA = smoothPath[currentPathIndex - 1];
        var pointB = smoothPath[currentPathIndex];
        var pointC = smoothPath[currentPathIndex + 1];
        
        var incomingDir = (pointB - pointA).Normalized();
        var outgoingDir = (pointC - pointB).Normalized();
        
        // Dot product returns 1.0 if straight, 0.0 if 90 degrees, -1.0 if 180 degrees turn
        var dot = incomingDir.Dot(outgoingDir);
        DotLabel.Text = $"{dot}";
        
        // Map the dot product (-1.0 to 1.0) into a clean 0.0 to 1.0 curve penalty
        // 1.0 means perfectly straight line (No penalty)
        // 0.0 means complete 180 hairpin turn (Maximum penalty)
        var turnSmoothness = Mathf.Remap(dot, -1f, 1f, 0f, 1f);
        
        // Interpolate between our absolute minimum allowed turn speed and our maximum speed
        var targetSpeedForTurn = Mathf.Lerp(MaxSpeed * MinTurnSpeedPercentage, MaxSpeed, turnSmoothness);
        
        // --- LOOK AHEAD BRAKING TRIGGER ---
        // If the drone is getting close to the turn node, start forcing the slowdown.
        var distanceToTurn = GlobalPosition.DistanceTo(pointB);
        if (distanceToTurn < BreakingDistance)
        {
            // Smoothly transition from MaxSpeed to the turn speed as we close the distance
            var t = Mathf.Clamp(distanceToTurn / BreakingDistance, 0f, 1f);
            return Mathf.Lerp(targetSpeedForTurn, MaxSpeed, t);
        }

        return MaxSpeed;
    }

    private void DrawAStarPath()
    {
        if (aStarPath.Length == 0) return;
    
        DebugDraw3D.DrawSphere(aStarPath[0], 0.7f, Colors.Blue);
        DebugDraw3D.DrawSphere(aStarPath[^1], 0.7f, Colors.Red);
    
        for (int i = 0; i < aStarPath.Length; i++)
        {
            DebugDraw3D.DrawSphere(aStarPath[i], 0.5f, i == currentPathIndex ? Colors.Gold : Colors.Green);

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
            DebugDraw3D.DrawSphere(smoothPath[i], 0.5f, i == currentPathIndex ? Colors.Gold : Colors.Green);
            
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
