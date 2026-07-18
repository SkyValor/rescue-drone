namespace RescueDrone;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Mover : CharacterBody3D
{
    public enum EnemyState { Idle, Patrol, Seek, Stunned, Random }
    
    public enum EnemyPatrolState { IntoCircuit, ToNextWaypoint, Lookout }
    
    public enum EnemySeekState { Stay, Chase, BackAway }
    
    [ExportGroup("Speed Settings")]
    [Export] public float MaxSpeed { get; private set; } = 15f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    [Export] public float Acceleration { get; private set; } = 5f;
    [Export] public float Deceleration { get; private set; } = 8f;
    
    [ExportGroup("Momentum Settings")]
    [Export] public float BreakingDistance { get; private set; } = 6f;
    [Export] public float MinTurnSpeedPercentage { get; private set; } = 0.25f;
    
    [ExportGroup("SVO")]
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    [Export] public float DroneRadius { get; private set; } = 1f;
    [Export] public float TargetRadius { get; private set; } = 2f;

    [ExportGroup("Player Seeking Settings")]
    [Export] public float MinDistance { get; private set; } = 4f;
    [Export] public float MaxDistance { get; private set; } = 7f;
    [Export] public float RepathThreshold { get; private set; } = 2f; // Only recalculate SVO path if player moves this much
    [Export] public PlayerMover Player { get; private set; }
    
    [ExportGroup("Patrol")]
    [Export] public Node3D[] PatrolWaypoints { get; private set; }  
    // TODO: Turn this into circuits with connections
    // Make this available in the GameRepo and remove from here.
    
    // DEBUGGING
    [ExportGroup("Debugging")]
    [Export] public Label SpeedLabel { get; private set; }
    [Export] public Label DotLabel { get; private set; }
    [Export] public Label TargetLabel { get; private set; }
    [Export] public Label TargetVelocityLabel { get; private set; }

    private Array<Rid> selfRID;
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
    private EnemySeekState currentSeekState = EnemySeekState.Stay;
    private EnemyPatrolState currentPatrolState = EnemyPatrolState.IntoCircuit;
    
    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        selfRID = [GetRid()];
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
        
        if (PathLinesToDraw()) DrawPathLines();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey inputEventKey) return;
        
        switch (inputEventKey.Keycode)
        {
            case Key.F1:
                currentState = EnemyState.Idle;
                CleanState();
                break;
            case Key.F2:
                currentState = EnemyState.Patrol;
                CleanState();
                break;
            case Key.F3:
                currentState = EnemyState.Seek;
                CleanState();
                break;
            case Key.F5:
                currentState = EnemyState.Random;
                CleanState();
                break;
        }
    }
    
    private void CleanState()
    {
        aStarPath = null;
        smoothPath = null;
        waypointsInvestigated.Clear();
    }
    
    #region Physics
    
    public override void _PhysicsProcess(double delta)
    {
        var deltaTime = (float) delta;
        switch (currentState)
        {
            case EnemyState.Idle:
                break;
            
            case EnemyState.Patrol:
                PhysicsTickPatrol(deltaTime);
                break;
            case EnemyState.Seek:
                PhysicsTickSeek(deltaTime);
                break;
            case EnemyState.Random:
                PhysicsTickRandomMovement(deltaTime);
                break;
        }
    }
    
    private void PhysicsTickPatrol(float deltaTime) 
    {
        if (currentWaypoint is null)
        {
            // Current waypoint is null at the start of Patrol state.
            currentWaypoint = GetClosestWaypoint();
            RecalculatePathToWaypoint();
            return;
        }
        
        if (!PathLinesToDraw())
        {
            // There are no lines to draw if pathfinding was not able to complete.
            RecalculatePathToWaypoint();
            return;
        }

        var toWaypoint = currentWaypoint.GlobalPosition - GlobalPosition;
        var distanceToWaypoint = toWaypoint.Length();
        if (distanceToWaypoint < 1.5f)
        {
            waypointsInvestigated.Add(currentWaypoint);
            if (waypointsInvestigated.Count > 4)
                waypointsInvestigated.RemoveAt(0);

            currentWaypoint = PatrolWaypoints
                .Where(wp => !waypointsInvestigated.Contains(wp))
                .OrderBy(wp => wp.GlobalPosition.DistanceTo(GlobalPosition))
                .FirstOrDefault();

            if (currentWaypoint is null)
            {
                GD.PrintErr("Was not able to get the closest not investigated waypoint.");
                return;
            }
            
            RecalculatePathToWaypoint();
            return;
        }

        var targetPosition = smoothPath[currentPathIndex];
        var toTarget = targetPosition - GlobalPosition;
        
        // TODO: When reaching the desired waypoint, commence Coroutine to lookout for player drone.
        // When completed, increment currentPathIndex and allow the flow to recalculate the new path.

        var desiredSpeed = MaxSpeed * 0.25f;
        var speedBleedingFactor = desiredSpeed < currentTargetSpeed ? Deceleration : Acceleration;
        currentTargetSpeed = Mathf.Lerp(currentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);

        var direction = toWaypoint.Normalized();
        SmoothlyRotate(direction, deltaTime);

        var targetVelocity = direction * currentTargetSpeed;
        Velocity = Velocity.Lerp(targetVelocity, Acceleration * deltaTime);
        MoveAndSlide();
    }

    private void PhysicsTickSeek(float deltaTime)
    {
        var toPlayer = Player.GlobalPosition - GlobalPosition;
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
        
        // TODO: CAREFUL!! We might be recalculating a path every frame.
        
        var idealTarget = CalculatePursuitTarget();
        if (idealTarget.DistanceTo(lastTargetPosition) <= RepathThreshold) return;

        lastTargetPosition = idealTarget;
        var startLeaf = svo.FindClosestEmptyLeaf(GlobalPosition);
        var targetLeaf = svo.FindClosestEmptyLeaf(idealTarget);

        if (startLeaf is not null && targetLeaf is not null && targetLeaf.IsEmpty)
        {
            aStarPath = svo.CreatePath(startLeaf.Id, targetLeaf.Id);
            smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, selfRID);
            currentPathIndex = 0;
        }
    }

    private void PhysicsTickRandomMovement(float deltaTime)
    {
        if (svo is null) return;
        if (smoothPath is null || smoothPath.Length == 0 || currentPathIndex >= smoothPath.Length)
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
        TargetVelocityLabel.Text = $"{targetVelocity.Length()}";
        Velocity = Velocity.Lerp(targetVelocity, Acceleration * deltaTime);
        MoveAndSlide();
    }
    
    #endregion
    
    private bool PathLinesToDraw() => aStarPath is not null && smoothPath is not null;
    
    private Node3D GetRandomWaypoint()
    {
        var rand = GD.RandRange(0, PatrolWaypoints.Length - 1);
        return PatrolWaypoints[rand];
    }

    private Node3D GetClosestWaypoint()
    {
        Node3D waypoint = null;
        var distance = double.MaxValue;
        foreach (var wp in PatrolWaypoints)
        {
            var dist = GlobalPosition.DistanceTo(wp.GlobalPosition);
            if (dist > distance) continue;
            
            distance = dist;
            waypoint = wp;
        }

        return waypoint;
    }

    private void RecalculatePath()
    {
        if (dronePathfinding is null) return;
        
        aStarPath = dronePathfinding.GetAStarPath(svo, GetWorld3D(), GlobalPosition, DroneRadius);
        smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, selfRID);
        currentPathIndex = 0;
        currentTargetSpeed = MaxSpeed;
    }

    private bool RecalculatePathToWaypoint()
    {
        return currentWaypoint is not null && RecalculatePath(startPosition: GlobalPosition, endPosition: currentWaypoint.GlobalPosition);
    }

    private bool RecalculatePath(Vector3 startPosition, Vector3 endPosition)
    {
        if (svo is null)
        {
            GD.PrintErr("SVO system not set. Cannot complete drone pathfinding.");
            return false;
        }

        if (dronePathfinding is null)
        {
            GD.PrintErr("Drone pathfinding strategy not set.");
            return false;
        }
        
        var startLeaf = svo.FindClosestEmptyLeaf(startPosition);
        var targetLeaf = svo.FindClosestEmptyLeaf(endPosition);
        if (startLeaf is null || targetLeaf is null || !targetLeaf.IsEmpty) 
            return false;

        aStarPath = svo.CreatePath(startLeaf.Id, targetLeaf.Id);
        smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, selfRID);
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

    private void UpdateAIChaseBehavior()
    {
        var idealTarget = CalculatePursuitTarget();
        if (idealTarget.DistanceTo(lastTargetPosition) <= RepathThreshold) return;

        lastTargetPosition = idealTarget;
        var startLeaf = svo.FindClosestEmptyLeaf(GlobalPosition);
        var targetLeaf = svo.FindClosestEmptyLeaf(idealTarget);

        if (startLeaf is not null && targetLeaf is not null && targetLeaf.IsEmpty)
        {
            aStarPath = svo.CreatePath(startLeaf.Id, targetLeaf.Id);
            smoothPath = dronePathfinding.SmoothPath(aStarPath, GetWorld3D(), DroneRadius, selfRID);
            currentPathIndex = 0;
        }
    }

    private Vector3 CalculatePursuitTarget()
    {
        var playerPosition = Player.GlobalPosition;
        var toPlayer = playerPosition - GlobalPosition;
        var distance = toPlayer.Length();
        var targetDistance = Mathf.Clamp(distance, MinDistance, MaxDistance);
        return playerPosition - toPlayer.Normalized() * targetDistance;
        
        
        // var playerPosition = Player.GlobalPosition;
        // var toEnemy = GlobalPosition - playerPosition;
        // var distance = toEnemy.Length();
        // if (distance < 0.1f) 
        //     return playerPosition + new Vector3(0, 0, MinDistance);
        //
        // var targetDistance = Mathf.Clamp(distance, MinDistance, MaxDistance);
        // return playerPosition + (toEnemy * targetDistance);
    }
    
    #region Draw Functions

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
            return;
        }

        for (int i = 0; i < aStarPath.Length - 1; i++)
            DebugDraw3D.DrawLine(aStarPath[i], aStarPath[i + 1], Colors.Green);
        
        for (int j = 0; j < smoothPath.Length - 1; j++)
            DebugDraw3D.DrawLine(smoothPath[j], smoothPath[j + 1], Colors.Goldenrod);
    }
    
    #endregion
    
}
