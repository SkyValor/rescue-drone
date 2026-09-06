namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Chickensoft.Sync.Primitives;
using Godot;

public interface IEnemyAIDrone : IFlyingDrone
{
	float DroneRadius { get; }
}

[Meta(typeof(IAutoNode))]
public partial class EnemyAIDrone : CharacterBody3D, IEnemyAIDrone
{
	public override void _Notification(int what) => this.Notify(what);
	
	[Export] public bool StartInPatrol { get; set; }
	[Export] public bool StayInPatrol { get; set; }
	
	[Export(PropertyHint.ResourceType, "EnemyDroneSettings")]
	public EnemyDroneSettings Settings { get; private set; } = new();

	[Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>(() => null); // TODO: Temporary solution; remove later
	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();

	[Node] private CollisionShape3D Collider { get; set; }
	[Node] private SightSensor Sight { get; set; }
	
	#region AI State Machine
	public EnemyAILogic AIStateMachine { get; private set; }
	private LogicBlock<EnemyAILogic.State>.IBinding AIStateBinding { get; set; }
	#endregion

	private AutoValue<SparseVoxelOctree>.Binding octreeBind;
	private IPathfindSVO pathfinder;
	
	public float DroneRadius => Collider.Shape is not SphereShape3D sphereShape ? 0f : sphereShape.Radius;

	public void OnReady()
	{
		SetPhysicsProcess(false);
	}

	public void OnResolved()
	{
		var octree = GameRepo.SVO.Value;
		if (octree is null)
		{
			octreeBind = GameRepo.SVO.Bind();
			octreeBind.OnValue(StartAIStateMachine);
		}
		else
		{
			StartAIStateMachine(octree);
		}
	}

	public void OnPhysicsProcess(double delta)
	{
		AIStateMachine.Input(new EnemyAILogic.Input.PhysicsTick(delta));

		MoveAndSlide();
		AIStateMachine.Input(new EnemyAILogic.Input.Moved());
	}

	private void StartAIStateMachine(SparseVoxelOctree octree)
	{
		if (octree is null) return;

		octreeBind?.Dispose();

		pathfinder = new VoxelOctreeAStar(GameRepo.SVO.Value);
		var data = new EnemyAILogic.Data
		{
			StartInPatrol = StartInPatrol,
			StayInPatrol = StayInPatrol
		};

		AIStateMachine = new EnemyAILogic();
		AIStateMachine.Set(this);
		AIStateMachine.Set(GetWorld3D());
		AIStateMachine.Set(pathfinder);
		AIStateMachine.Set(Settings);
		AIStateMachine.Set(Sight);
		AIStateMachine.Set(data);
		
		AIStateBinding = AIStateMachine.Bind();
		AIStateBinding
			.Handle((in EnemyAILogic.Output.RotationComputed output) => GlobalTransform = output.GlobalTransform)
			.Handle((in EnemyAILogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		
		AIStateMachine.Start();
	}
	
}
