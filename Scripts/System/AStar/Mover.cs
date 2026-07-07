namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class Mover : CharacterBody3D
{
    public enum EnemyState { Idle, Patrol, Seek, Stunned }
    
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

    [ExportGroup("Player Seeking Settings")]
    [Export] public float MinDistance { get; private set; } = 4f;
    [Export] public float MaxDistance { get; private set; } = 7f;
    [Export] public float RepathThreshold { get; private set; } = 2f; // Only recalculate SVO path if player moves this much
    
    [Export] public Node3D[] PatrolWaypoints { get; private set; }
    
    // DEBUGGING
    [Export] public Label SpeedLabel { get; private set; }
    [Export] public Label DotLabel { get; private set; }
    [Export] public Label TargetLabel { get; private set; }
    [Export] public PlayerMover Player { get; private set; }

    private Array<Rid> exclusion;
    private DronePathfinding dronePathfinding;
    private SparseVoxelOctreeShape svo;
    private Vector3[] aStarPath = [];
    private Vector3[] smoothPath = [];
    private int currentPathIndex;
    private float currentTargetSpeed;
    private Vector3 lastTargetPosition;
    private Node3D currentWaypoint;
    private readonly List<Node3D> waypointsInvestigated = [];
    private EnemyState currentState = EnemyState.Idle;

    private bool drawPathLines;
    
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
        // SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        // DrawAStarPath();
        // DrawSmoothPath();
        
        if (drawPathLines) DrawPathLines();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey inputEventKey) return;
        
        switch (inputEventKey.Keycode)
        {
            case Key.F1:
                currentState = EnemyState.Idle;
                SetPhysicsProcess(false);
                drawPathLines = false;
                waypointsInvestigated.Clear();
                break;
            case Key.F2:
                currentState = EnemyState.Patrol;
                Node3D waypoint;
                do { waypoint = GetRandomWaypoint(); } 
                while (waypointsInvestigated.Contains(waypoint));
                waypointsInvestigated.Add(waypoint);
                if (waypointsInvestigated.Count > 4)
                    waypointsInvestigated.RemoveAt(0);
                currentWaypoint = waypoint;
                var start = svo.FindClosestEmptyLeaf(GlobalPosition);
                var end = svo.FindClosestEmptyLeaf(currentWaypoint.GlobalPosition);
                if (RecalculatePath(start.Position, end.Position))
                {
                    SetPhysicsProcess(true);
                    drawPathLines = true;
                }
                break;
        }
    }

    private Node3D GetRandomWaypoint()
    {
        var rand = GD.RandRange(0, PatrolWaypoints.Length - 1);
        return PatrolWaypoints[rand];
    }
    
    public override void _PhysicsProcess(double delta)
    {
        var deltaTime = (float) delta;

        var toPlayer = GlobalPosition.DirectionTo(Player.GlobalPosition);
        var distanceToPlayer = toPlayer.Length();
        if (distanceToPlayer < MinDistance)
        {
            // The player is too close! Back away slowly instead of following the path.
            var backAwayDirection = -toPlayer.Normalized();
            var backingVelocity = backAwayDirection * (MaxSpeed * 0.5f);

            Velocity = Velocity.Lerp(backingVelocity, Deceleration * deltaTime);
            MoveAndSlide();
            return;
        }
        
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
        
        var desiredSpeed = CalculateAdaptiveSpeed();
        TargetLabel.Text = $"{desiredSpeed}";
        var speedBleedingFactor = desiredSpeed < currentTargetSpeed ? Deceleration : Acceleration;
        currentTargetSpeed = Mathf.Lerp(currentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);
        SpeedLabel.Text = $"{currentTargetSpeed}";
        
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

    private bool RecalculatePath(Vector3 startPosition, Vector3 endPosition)
    {
        var startLeaf = svo.FindClosestEmptyLeaf(startPosition);
        var targetLeaf = svo.FindClosestEmptyLeaf(endPosition);
        if (startLeaf is null || targetLeaf is null || !targetLeaf.IsEmpty) 
            return false;

        aStarPath = svo.CreatePath(startLeaf.Id, targetLeaf.Id);
        smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, exclusion);
        currentPathIndex = 0;
        return true;
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

    public void UpdateAIChaseBehavior(Vector3 playerPosition, World3D world, float droneRadius)
    {
        var idealTarget = CalculatePursuitTarget(playerPosition, GlobalPosition);
        if (idealTarget.DistanceTo(lastTargetPosition) <= RepathThreshold) return;

        lastTargetPosition = idealTarget;
        var startLeaf = svo.FindClosestEmptyLeaf(GlobalPosition);
        var targetLeaf = svo.FindClosestEmptyLeaf(idealTarget);

        if (startLeaf is not null && targetLeaf is not null && targetLeaf.IsEmpty)
        {
            aStarPath = svo.CreatePath(startLeaf.Id, targetLeaf.Id);
            smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, exclusion);
            currentPathIndex = 0;
        }
    }

    private Vector3 CalculatePursuitTarget(Vector3 playerPosition, Vector3 enemyPosition)
    {
        var toEnemy = enemyPosition - playerPosition;
        var currentDistance = toEnemy.Length();
        if (currentDistance < 0.1f) 
            return playerPosition + new Vector3(0, 0, MinDistance);

        var directionFromPlayer = toEnemy.Normalized();
        var targetDistance = Mathf.Clamp(currentDistance, MinDistance, MaxDistance);
        return playerPosition + (directionFromPlayer * targetDistance);
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
