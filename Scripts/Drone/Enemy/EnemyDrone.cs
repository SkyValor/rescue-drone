namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;
using Godot.Collections;

public interface IEnemyDrone : ICharacterBody3D
{
	DroneMovementByPosition DroneMovement { get; set; }
}

[Meta(typeof(IAutoNode), typeof(IProvider))]
public partial class EnemyDrone : CharacterBody3D, IEnemyDrone, IProvide<EnemyDrone>
{
	public override void _Notification(int what) => this.Notify(what);
	
	#region Exports
	[ExportGroup("Drone Movement Stats")]
	[Export] private float SpringStrength { get; set; } = 12f;
	[Export] private float Damping { get; set; } = 8f;
	[Export] private float MaxSpeed { get; set; } = 10f;
	[Export] private float PlayerMinDistance { get; set; } = 3f;	

	[Export] private float OscillationMagnitude { get; set; } = 0.05f;
	[Export] private float OscillationHeight { get; set; } = 0.5f;
	
	[Export] private float AvoidanceStrength { get; set; } = 20f;
	[Export] private float AvoidanceDistance { get; set; } = 4f;
	
	[ExportGroup("Drone Movement Waypoints")]
	[Export] private Array<Waypoint> Waypoints { get; set; }
	[Export] private float VisionRange { get; set; }
	[Export] private float VisionAngle { get; set; } = 60f;
	[Export] private float VisionAngleToAttack { get; set; } = 20f;
	
	[Export] private int VisionMaskEnvironment { get; set; }
	
	[ExportGroup("Patrol State")]
	[Export] private float LookoutAngle { get; set; } = 45f;
	[Export] private float LookoutRotationTime { get; set; } = 1f;
	[Export] private float LookoutHoldDuration { get; set; }

	[ExportGroup("Search State")] 
	[Export] private float SearchLookoutAngle { get; set; } = 210f;
	[Export] private float SearchLookoutRotationTime { get; set; } = 1f;
	[Export] private float SearchLookoutHoldDuration { get; set; } = 0.75f;

	[ExportGroup("Attack State")] 
	[Export] private float AttackDistance { get; set; } = 2.5f;
	#endregion
	
	#region Nodes
	[Node] public DroneMovementByPosition DroneMovement { get; set; }
	[Node] public EnemyWeapon WeaponComponent { get; set; }
	[Node] public Area3D VisionArea { get; private set; }
	[Node] public RayCast3D VisionRaycast { get; private set; }
	[Node] private MeshInstance3D MeshInstance3D { get; set; }
	#endregion

	EnemyDrone IProvide<EnemyDrone>.Value() => this;

	#region State Machine
	public EnemyLogic EnemyStateMachine { get; set; }
	private LogicBlock<EnemyLogic.State>.IBinding EnemyBinding { get; set; }
	#endregion
	
	public override void _Ready()
	{
		EnemyStateMachine = new EnemyLogic();
		var player = GetTree().GetNodesInGroup("player")[0] as Drone;
		if (player is null) 
			return;
		
		this.Provide();

		var settings = new EnemyLogic.Settings(VisionRange, PlayerMinDistance, 
			LookoutAngle, LookoutRotationTime, LookoutHoldDuration,
			SearchLookoutAngle, SearchLookoutRotationTime, SearchLookoutHoldDuration);
		
		EnemyStateMachine.Set(this);
		EnemyStateMachine.Set(Waypoints);
		EnemyStateMachine.Set(settings);
		EnemyStateMachine.Set(player);
		EnemyStateMachine.Set(new EnemyLogic.Data());
		
		EnemyBinding = EnemyStateMachine.Bind();
		EnemyBinding.Handle((in EnemyLogic.Output.MoveTowards output) =>
		{
			DroneMovement.MoveTo(output.TargetPosition, output.Delta);
			DroneMovement.RotateSmoothlyTo(output.TargetPosition, output.Delta);
		});
		
		EnemyStateMachine.Start();
	}

	public override void _ExitTree()
	{
		EnemyStateMachine.Stop();
		EnemyBinding.Dispose();
	}

	public override void _PhysicsProcess(double delta)
	{
		EnemyStateMachine.Input(new EnemyLogic.Input.PhysicsTick((float) delta));
	}

	#region Attack state

	private void AttackOnSight(double delta)
	{
		// var playerPosition = player.GlobalPosition;
		// var currentDistance = GlobalPosition.DistanceTo(playerPosition);
		// if (currentDistance > AttackDistance)
		// {
		// 	DroneMovement.MoveTowards(player, delta);
		// }
		// else if (currentDistance < PlayerMinDistance)
		// {
		// 	DroneMovement.MoveAwayFrom(player, delta);
		// }
		//
		// // Check if enemy is looking at player.
		// var directionToPlayer = (playerPosition - GlobalPosition).Normalized();
		// var forwardDirection = -GlobalTransform.Basis.Z with { Y = 0f };
		// var angle = Mathf.RadToDeg(forwardDirection.AngleTo(directionToPlayer));
		// GD.Print("Angle is " + angle);
		// if (angle < 30f / 2)
		// {
		// 	// Enemy is looking at player.
		// 	WeaponComponent.TryShooting(playerPosition);
		// }
		
		// playerLastKnownPosition = playerPosition;
		
		// if (GlobalPosition.DistanceTo(playerPosition) < AttackDistance - 1f)
		// {
		// 	// Drone is too close to player, move back
		// 	DroneMovement.MoveTowards(player, delta);
		// }
		// else
		// {
		// 	// Drone is too far from player, move forward
		// 	DroneMovement.MoveAwayFrom(player, delta);
		// }
		
		
		
		
		// if (GlobalPosition.DistanceTo(lastKnownPlayerPosition) >= PlayerMinDistance)
		// DroneMovement.MoveTo(lastKnownPlayerPosition, (float) Timing.DeltaTime);

		// if (PlayerInVisionRange(VisionAngleToAttack))
		// {
		// 	// Shoot the player
		// 	GD.Print("Shooting");
		// 	WeaponComponent.TryShooting();
		// }
	}
	
	#endregion
	
}
