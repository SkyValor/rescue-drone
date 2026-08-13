namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IEnemyAIDrone : IFlyingDrone
{
    float DroneRadius { get; }
}

[Meta(typeof(IAutoNode))]
public partial class EnemyAIDrone : CharacterBody3D, IEnemyAIDrone
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export(PropertyHint.ResourceType, "EnemyDroneSettings")]
    public EnemyDroneSettings Settings { get; private set; } = new();
    
    [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();

    [Node] private CollisionShape3D Collider { get; set; }
    [Node] private SightSensor Sight { get; set; }
    
    #region AI State Machine
    public EnemyAILogic AIStateMachine { get; private set; }
    private LogicBlock<EnemyAILogic.State>.IBinding AIStateBinding { get; set; }
    #endregion

    private IPathfindSVO pathfinder;
    
    public float DroneRadius => Collider.Shape is not SphereShape3D sphereShape ? 0f : sphereShape.Radius;

    public void OnReady()
    {
        SetPhysicsProcess(false);
    }

    public void OnResolved()
    {
        pathfinder = new VoxelOctreeAStar(GameRepo.SVO.Value);
        
        AIStateMachine = new EnemyAILogic();
        AIStateMachine.Set(this);
        AIStateMachine.Set(GetWorld3D());
        AIStateMachine.Set(pathfinder);
        AIStateMachine.Set(Settings);
        AIStateMachine.Set(Sight);
        
        AIStateBinding = AIStateMachine.Bind();
        AIStateBinding
            .Handle((in EnemyAILogic.Output.RotationComputed output) => GlobalTransform = output.GlobalTransform)
            .Handle((in EnemyAILogic.Output.VelocityComputed output) => Velocity = output.Velocity);
        
        AIStateMachine.Start();
    }

    public void OnPhysicsProcess(double delta)
    {
        AIStateMachine.Input(new EnemyAILogic.Input.PhysicsTick(delta));

        MoveAndSlide();
        AIStateMachine.Input(new EnemyAILogic.Input.Moved());
    }
    
}
