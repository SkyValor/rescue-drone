namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class EnemyAIDrone : CharacterBody3D, IFlyingDrone
{
	public override void _Notification(int what) => this.Notify(what);
	
	[Export] public bool StartInPatrol { get; set; }
	[Export] public bool StayInPatrol { get; set; }
	
	[Export(PropertyHint.ResourceType, "EnemyDroneSettings")]
	public EnemyDroneSettings Settings { get; private set; } = new();

	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();

	[Node] private CollisionShape3D Collider { get; set; }
	[Node] private SightSensor Sight { get; set; }
	
	#region AI State Machine
	private EnemyAILogic AIStateMachine { get; set; }
	private EnemyAILogic.IBinding AIStateBinding { get; set; }
	#endregion

	private AutoValue<SparseVoxelOctree>.Binding octreeBind;
	
	public float DroneRadius => Collider.Shape is not SphereShape3D sphereShape ? 0f : sphereShape.Radius;

	public void OnEnterTree()
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

	public void OnExitTree()
	{
		AIStateBinding.Dispose();
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

		IPathfindSVO pathfinder = new VoxelOctreeAStar(GameRepo.SVO.Value);

		AIStateMachine = new EnemyAILogic();
		AIStateMachine.Set(new EnemyAILogic.Data());
		AIStateMachine.Set(GetWorld3D());
		AIStateMachine.Set(pathfinder);
		AIStateMachine.Set(GameRepo);
		AIStateMachine.Set(Settings);
		AIStateMachine.Set(Sight);
		AIStateMachine.Set(this);
		
		AIStateBinding = AIStateMachine.Bind();
		AIStateBinding
			.Handle((in EnemyAILogic.Output.RotationComputed output) => GlobalTransform = output.GlobalTransform)
			.Handle((in EnemyAILogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		
		AIStateMachine.Start();
		SetPhysicsProcess(true);
	}
	
}
