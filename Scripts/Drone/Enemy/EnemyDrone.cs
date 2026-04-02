namespace RescueDrone;

using System.Collections.Generic;
using System.Diagnostics;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;
using Godot.Collections;
using MEC;

public interface IEnemyDrone : ICharacterBody3D
{
	DroneMovementByPosition DroneMovement { get; set; }
}

[Meta(typeof(IAutoNode), typeof(IProvider))]
public partial class EnemyDrone : CharacterBody3D, IEnemyDrone, IProvide<EnemyDrone>
{
	public override void _Notification(int what) => this.Notify(what);
	
	public enum EnemyState
	{
		Idle,
		Patrol,
		Attacking,
		Searching
	}

	public enum PatrolState
	{
		MoveToWaypoint,
		Lookout
	}
	
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
	[Export] private float LookoutAngle { get; set; } = 45f;
	[Export] private float LookoutRotationTime { get; set; } = 1f;
	[Export] private float LookoutHoldDuration { get; set; }

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
	
	private EnemyState currentState = EnemyState.Idle;
	private PatrolState patrolState;
	private Waypoint currentWaypoint;
	private Waypoint previousWaypoint;
	private Drone player;
	private Vector3 playerLastKnownPosition;
	private bool playerOnSight;

	private bool isMovingToWaypoint;
	private bool isOnLookout;
	private bool isAttacking;

	private CoroutineHandle moveToWaypointCoroutine;
	private CoroutineHandle lookoutCoroutine;

	public override void _Ready()
	{
		EnemyStateMachine = new EnemyLogic();
		player = GetTree().GetNodesInGroup("player")[0] as Drone;
		if (player is null) 
			return;
		
		this.Provide();

		var settings = new EnemyLogic.Settings(VisionRange, PlayerMinDistance, 
			LookoutAngle, LookoutRotationTime, LookoutHoldDuration);
		
		EnemyStateMachine.Set(this);
		EnemyStateMachine.Set(Waypoints);
		EnemyStateMachine.Set(settings);
		EnemyStateMachine.Set(player);
		
		EnemyBinding = EnemyStateMachine.Bind();
		// EnemyBinding
		// .Handle((in EnemyLogic.Output.VelocityChanged output) =>
		// Velocity = output.Velocity);
		// .Handle((in EnemyLogic.Output.RotationRequest output) =>
		// DroneMovement.RotateToTarget(output.TargetRotation, output.Delta));
		EnemyStateMachine.Start();
	}

	public override void _ExitTree()
	{
		EnemyStateMachine.Stop();
		EnemyBinding.Dispose();
	}

	// public override void _Process(double delta)
	// {
	// 	var debugColor = playerOnSight ? Colors.Green : Colors.Brown;
	// 	DebugDraw3D.DrawLine(GlobalPosition, playerLastKnownPosition, debugColor);
	// }

	public override void _PhysicsProcess(double delta)
	{
		EnemyStateMachine.Input(new EnemyLogic.Input.PhysicsTick((float) delta));
	}

	// public void OnPhysicsProcess(double delta)
	// {
	// 	playerOnSight = HasLineOfSight();
	// 	if (playerOnSight)
	// 		playerLastKnownPosition = player.GlobalPosition;
	// 	
	// 	LookAtPlayerKnownPosition(delta);
	// 	
	// 	switch (currentState)
	// 	{
	// 		case EnemyState.Idle:
	// 			ProcessIdle();
	// 			break;
	// 		case EnemyState.Patrol:
	// 			if (patrolState is PatrolState.MoveToWaypoint)
	// 			{
	// 				ProcessMoveToWaypoint(delta);
	// 			}
	// 			break;
	// 		case EnemyState.Attacking:
	// 			ProcessAttack(delta);
	// 			break;
	// 	}
	// }

	#region Idle state

	private void ProcessIdle()
	{
		currentState = HasLineOfSight() ? EnemyState.Attacking : EnemyState.Patrol;
		if (currentState is EnemyState.Patrol)
			patrolState = PatrolState.MoveToWaypoint;
	}
	
	#endregion

	#region Patrol state

	private void ProcessPatrol(double delta)
	{
		if (HasLineOfSight())
		{
			GD.Print("Line of sight discovers player. Engaging...");
			Timing.KillCoroutines(moveToWaypointCoroutine);
			Timing.KillCoroutines(lookoutCoroutine);

			isMovingToWaypoint = false;
			isOnLookout = false;
			currentWaypoint = null;
			previousWaypoint = null;
			
			currentState = EnemyState.Attacking;
			return;
		}

		if (isMovingToWaypoint || isOnLookout)
			return;
		
		if (currentWaypoint is null)
		{
			InitiatePatrolPath();
			return;
		}

		var distanceToWaypoint = GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
		if (distanceToWaypoint > 0.05f) 
		{
			DroneMovement.MoveTo(currentWaypoint.GlobalPosition, (float) delta);
		}
		else
		{
			InitiateLookout();
		}
	}

	private void ProcessMoveToWaypoint(double delta)
	{
		currentWaypoint ??= GetClosestWaypoint();
		
		var distanceToWaypoint = GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
		if (distanceToWaypoint > 0.05f) 
		{
			DroneMovement.MoveTo(currentWaypoint.GlobalPosition, (float) delta);
		}
		else
		{
			// InitiateLookout();
			patrolState = PatrolState.Lookout;
			lookoutCoroutine = Timing.RunCoroutine(LookoutCoroutine().CancelWith(this), Segment.PhysicsProcess);
		}
	}
	
	private void InitiatePatrolPath() 
	{
		currentWaypoint = GetClosestWaypoint();
		MoveToWaypoint();
	}
	
	// TODO: Make HasLineOfSight its own coroutine and be running while enemy is not Attacking.
	// When the enemy detects the player, break out of this coroutine and change the state. This will break out of any other coroutines.

	private void MoveToWaypoint()
	{
		moveToWaypointCoroutine = Timing.RunCoroutine(MoveToWaypointCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}
	
	private IEnumerator<double> MoveToWaypointCoroutine()
	{
		while (GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition) > 0.05f)
		{
			DroneMovement.MoveTo(currentWaypoint.GlobalPosition, (float) Timing.DeltaTime);
			yield return Timing.WaitForOneFrame;
		}

		isMovingToWaypoint = false;
	}
	
	private void InitiateLookout()
	{
		isOnLookout = true;
		lookoutCoroutine = Timing.RunCoroutine(LookoutCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}

	private IEnumerator<double> LookoutCoroutine()
	{
		const double tweenDuration = 1.0;
		var initialY = GlobalRotationDegrees.Y;
		var targetRotation = GlobalRotationDegrees with { Y = initialY + LookoutAngle };
		
		// Rotate smoothly to the left and wait
		var rotationTween = CreateTween();
		rotationTween.TweenProperty(this, "rotation_degrees", targetRotation, tweenDuration);
		rotationTween.Play();
		yield return Timing.WaitForSeconds(tweenDuration + LookoutHoldDuration);

		// Rotate smoothly to the right and wait
		targetRotation = GlobalRotationDegrees with { Y = initialY - LookoutAngle };
		rotationTween = CreateTween();
		rotationTween.TweenProperty(this, "rotation_degrees", targetRotation, tweenDuration * 2f);
		yield return Timing.WaitForSeconds(tweenDuration * 2f + LookoutHoldDuration);

		isOnLookout = false;
		GoToNextWaypoint();
	}
	
	private void GoToNextWaypoint()
	{
		var nextWaypoint = GetNextWaypoint();
		previousWaypoint = currentWaypoint;
		currentWaypoint = nextWaypoint;
		MoveToWaypoint();
	}
	
	private Waypoint GetClosestWaypoint()
	{
		Waypoint closestWaypoint = null;
		var distanceToWaypoint = float.MaxValue;
		foreach (var waypoint in Waypoints)
		{
			var distance = GlobalPosition.DistanceTo(waypoint.GlobalPosition);
			if (distance >= distanceToWaypoint) 
				continue;
				
			distanceToWaypoint = distance;
			closestWaypoint = waypoint;
		}
			
		return closestWaypoint;
	}
	
	private Waypoint GetNextWaypoint()
	{
		var connections = currentWaypoint.Connections.Duplicate();
		if (previousWaypoint is not null)
			connections.Remove(previousWaypoint);

		return connections.Count == 0 ? previousWaypoint : connections.PickRandom();
	}
	
	#endregion

	#region Attack state
	
	private void ProcessAttack(double delta)
	{
		if (HasLineOfSight())
		{
			AttackOnSight(delta);
		}
		else if (GlobalPosition.DistanceTo(playerLastKnownPosition) > 2f)
		{
			DroneMovement.MoveTo(playerLastKnownPosition, delta);
		}
		else
		{
			GD.Print("Lost sight of player. Going back to patrol state.");
			currentState = EnemyState.Patrol;
		}
	}

	private void AttackOnSight(double delta)
	{
		var playerPosition = player.GlobalPosition;
		LookAtPlayerKnownPosition(delta);

		var currentDistance = GlobalPosition.DistanceTo(playerPosition);
		if (currentDistance > AttackDistance)
		{
			DroneMovement.MoveTowards(player, delta);
		}
		else if (currentDistance < PlayerMinDistance)
		{
			DroneMovement.MoveAwayFrom(player, delta);
		}
		
		// Check if enemy is looking at player.
		var directionToPlayer = (playerPosition - GlobalPosition).Normalized();
		var forwardDirection = -GlobalTransform.Basis.Z with { Y = 0f };
		var angle = Mathf.RadToDeg(forwardDirection.AngleTo(directionToPlayer));
		GD.Print("Angle is " + angle);
		if (angle < 30f / 2)
		{
			// Enemy is looking at player.
			WeaponComponent.TryShooting(playerPosition);
		}
		
		playerLastKnownPosition = playerPosition;
		
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

	private IEnumerator<double> AttackCoroutine()
	{
		while (currentState is EnemyState.Attacking)
		{
			yield return Timing.WaitForOneFrame;

			if (HasLineOfSight())
			{
				playerLastKnownPosition = player.GlobalPosition;
				DroneMovement.MoveTo(playerLastKnownPosition, (float) Timing.DeltaTime);
			}
		}
	}
	
	#endregion
	
	private void LookAtPlayerKnownPosition(double delta)
	{
		var directionToPlayer = GlobalPosition.DirectionTo(playerLastKnownPosition);
		var rotation = Rotation;
		rotation.Y = (float) Mathf.LerpAngle(rotation.Y, Mathf.Atan2(-directionToPlayer.X, -directionToPlayer.Z), delta * 2f);
		Rotation = rotation;
	}
	
	private bool HasLineOfSight()
	{
		if (player is null)
			return false;

		return PlayerInRange() && PlayerInVisionRange() && NoBuildingInBetween();
	}

	private bool PlayerInRange()
	{
		var distanceToPlayer = player.GlobalPosition.DistanceTo(GlobalPosition);
		return distanceToPlayer < VisionRange;
	}

	private bool PlayerInVisionRange()
	{
		var forward = -Basis.Z;
		var directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
		return Mathf.RadToDeg(directionToPlayer.AngleTo(forward)) <= 90f / 2;
	}

	private bool NoBuildingInBetween()
	{
		VisionRaycast.LookAt(player.GlobalPosition, Vector3.Up);
		VisionRaycast.ForceRaycastUpdate();
		return !VisionRaycast.IsColliding();
	}
	
}
