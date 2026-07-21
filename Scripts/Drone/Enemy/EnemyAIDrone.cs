namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IEnemyAIDrone : ICharacterBody3D;

[Meta(typeof(IAutoNode))]
public partial class EnemyAIDrone : CharacterBody3D, IEnemyAIDrone
{
    public override void _Notification(int what) => this.Notify(what);
    
    #region Exports
    [ExportGroup("Speed Settings")]
    [Export] public float MaxSpeed { get; private set; } = 15f;
    [Export] public float Acceleration { get; private set; } = 5f;
    [Export] public float Deceleration { get; private set; } = 8f;
    [Export] public float TurnSpeed { get; private set; } = 5f;
    
    [ExportGroup("Momentum Settings")]
    [Export] public float BreakingDistance { get; private set; } = 6f;
    [Export] public float MinTurnSpeedPercentage { get; private set; } = 0.25f;
    
    [ExportGroup("Player Seeking Settings")]
    [Export] public float MinDistance { get; private set; } = 4f;
    [Export] public float MaxDistance { get; private set; } = 7f;
    [Export] public float RepathThreshold { get; private set; } = 2f; // Only recalculate SVO path if player moves this much
    
    [ExportGroup("SVO")]
    [Export] public OctreeGeneratorGroup OctreeGenerator { get; private set; }
    [Export] public float DroneRadius { get; private set; } = 1f;
    [Export] public float PointTargetRadius { get; private set; } = 2f;
    
    [ExportGroup("Patrol")]
    [Export] public Node3D[] PatrolWaypoints { get; private set; }
    [Export] public int NumberOfScans { get; private set; } = 5;
    [Export] public float ScanWaitTime { get; private set; } = 3f;
    #endregion
    
    #region Dependecies
    [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    #endregion

    #region Nodes
    [Node] private SightSensor Sight { get; set; }
    // [Node] private IDronePathfindingSVO svo { get; set; }
    #endregion
    
    #region AI State Machine
    public EnemyAILogic AIStateMachine { get; private set; }
    public EnemyAILogic.Settings Settings { get; private set; }
    private EnemyAILogic.IBinding AIStateBinding { get; set; }
    #endregion

    private IDronePathfindingSVO dronePathfinder;

    public void OnReady()
    {
        dronePathfinder = new DronePathfindingSVO(GameRepo.OctreeGenerator.Value, DroneRadius, GetWorld3D());
    }

    public void OnResolved()
    {
        Settings = new EnemyAILogic.Settings(
            DroneRadius,
            MaxSpeed, Acceleration, Deceleration, TurnSpeed, 
            BreakingDistance, MinTurnSpeedPercentage,
            NumberOfScans, ScanWaitTime);
        
        AIStateMachine = new EnemyAILogic();
        AIStateMachine.Set(this as IEnemyAIDrone);
        AIStateMachine.Set(GetWorld3D());
        AIStateMachine.Set(dronePathfinder);
        AIStateMachine.Set(Settings);
        AIStateMachine.Set(Sight);
        
        AIStateBinding
            .Handle((in EnemyAILogic.Output.RotationComputed output) => GlobalTransform = output.GlobalTransform)
            .Handle((in EnemyAILogic.Output.VelocityComputed output) => Velocity = output.Velocity);

        AIStateBinding = AIStateMachine.Bind();
    }

    public void OnPhysicsProcess(double delta)
    {
        AIStateMachine.Input(new EnemyAILogic.Input.PhysicsTick(delta));

        MoveAndSlide();
        AIStateMachine.Input(new EnemyAILogic.Input.Moved());
    }
    
}
