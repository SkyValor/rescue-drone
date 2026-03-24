namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Godot.Collections;
using MEC;

public interface IEnemyDrone : ICharacterBody3D
{
	DroneMovementByPosition DroneMovement { get; set; }
}

[Meta(typeof(IAutoConnect), typeof(IProvider))]
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
	
	[Export] private int VisionMask { get; set; }
	[Export] private float LookoutAngle { get; set; } = 45f;
	[Export] private float LookoutDuration { get; set; }

	#endregion
	
	[Node] public DroneMovementByPosition DroneMovement { get; set; }
	[Node] public EnemyWeapon WeaponComponent { get; set; }
	[Node] public CollisionShape3D VisionCollision { get; set; }

	EnemyDrone IProvide<EnemyDrone>.Value() => this;

	private EnemyLogic EnemyStateMachine { get; set; }
	private EnemyLogic.IBinding EnemyBinding { get; set; }

	private EnemyState currentState = EnemyState.Idle;
	private Waypoint currentWaypoint;
	private Waypoint previousWaypoint;
	private Drone player;
	private Vector3 lastKnownPlayerPosition;

	private bool isOnLookout;
	private bool movingToWaypoint;
	private bool isAttacking;
	
	private CoroutineHandle lookoutCoroutine;
	private CoroutineHandle moveToWaypointCoroutine;
	private CoroutineHandle attackCoroutine;
	
	public override void _Ready()
	{
		// EnemyStateMachine = new EnemyLogic();
		player = GetTree().GetNodesInGroup("player")[0] as Drone;
		this.Provide();
		
		//  EnemyStateMachine.Set(this as IEnemyDrone);
		//  EnemyStateMachine.Set(Waypoints);
		//  EnemyStateMachine.Set(DroneMovement);
		//  EnemyStateMachine.Set(new EnemyLogic.Settings(LookoutDuration: 4f, LookoutAngle: 35f));
		//
		//  EnemyBinding = EnemyStateMachine.Bind();
		//  EnemyBinding
		//  	.Handle((in EnemyLogic.Output.VelocityChanged output) =>
		//  		Velocity = output.Velocity)
		//  	.Handle((in EnemyLogic.Output.RotationRequest output) =>
		//  		DroneMovement.RotateToTarget(output.TargetRotation, output.Delta));
		// 	
		// EnemyStateMachine.Start();
	}

	public override void _ExitTree()
	{
		EnemyStateMachine.Stop();
		EnemyBinding.Dispose();
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (currentState)
		{
			case EnemyState.Idle:
				ProcessIdle();
				break;
			case EnemyState.Patrol:
				ProcessPatrol();
				break;
			case EnemyState.Attacking:
				ProcessAttack();
				break;
		}
	}
	
	#region Idle state

	private void ProcessIdle()
	{
		currentState = HasLineOfSight() ? EnemyState.Attacking : EnemyState.Patrol;
	}
	
	#endregion

	#region Patrol state

	private void ProcessPatrol()
	{
		if (HasLineOfSight())
		{
			GD.Print("Line of sight discovers player. Engaging...");
			StopMovingToWaypoint();
			StopLookout();
			
			// Cleanup state
			currentWaypoint = null;
			previousWaypoint = null;
			
			currentState = EnemyState.Attacking;
			return;
		}
		
		if (isOnLookout || movingToWaypoint) 
			return;

		if (currentWaypoint is null)
		{
			currentWaypoint = GetClosestWaypoint();
			MoveToWaypoint();
			return;
		}
		
		if (previousWaypoint is null)
		{
			// This is the first waypoint we travel to. Immediately travel to next one.
			var nextWaypoint = GetNextWaypoint();
			previousWaypoint = currentWaypoint;
			currentWaypoint = nextWaypoint;	// TODO: GetNextWaypoint() can return null
			MoveToWaypoint();
			return;
		}
		
		InitiateLookout();
	}
	
	// TODO: Make HasLineOfSight its own coroutine and be running while enemy is not Attacking.
	// When the enemy detects the player, break out of this coroutine and change the state. This will break out of any other coroutines.

	private void MoveToWaypoint()
	{
		movingToWaypoint = true;
		moveToWaypointCoroutine = Timing.RunCoroutine(MoveToWaypointCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}
	
	private void StopMovingToWaypoint()
	{
		movingToWaypoint = false;
		Timing.KillCoroutines(moveToWaypointCoroutine);
	}
	
	private IEnumerator<double> MoveToWaypointCoroutine()
	{
		while (GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition) > 0.05f)
		{
			DroneMovement.MoveTo(currentWaypoint.GlobalPosition, (float) Timing.DeltaTime);
			yield return Timing.WaitForOneFrame;
		}

		movingToWaypoint = false;
	}
	
	private void InitiateLookout()
	{
		isOnLookout = true;
		lookoutCoroutine = Timing.RunCoroutine(LookoutCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}

	private void StopLookout()
	{
		isOnLookout = false;
		Timing.KillCoroutines(lookoutCoroutine);
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
		yield return Timing.WaitForSeconds(tweenDuration + LookoutDuration);

		// Rotate smoothly to the right and wait
		targetRotation = GlobalRotationDegrees with { Y = initialY - LookoutAngle };
		rotationTween = CreateTween();
		rotationTween.TweenProperty(this, "rotation_degrees", targetRotation, tweenDuration * 2f);
		yield return Timing.WaitForSeconds(tweenDuration * 2f + LookoutDuration);

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
	
	private void ProcessAttack()
	{
		if (HasLineOfSight())
		{
			lastKnownPlayerPosition = player.GlobalPosition;

			// var velocity = new Vector3();
			// var selfHorizontalPosition = GlobalPosition with { Y = 0f };
			// var playerHorizontalPosition = player.GlobalPosition with { Y = 0f };

			// if (selfHorizontalPosition.DistanceTo(playerHorizontalPosition) >= PlayerMinDistance)
			// {
			// 	var direction = player.GlobalPosition - GlobalPosition;
			// 	velocity += direction.Normalized() * 
			// }
			
			if (GlobalPosition.DistanceTo(lastKnownPlayerPosition) >= PlayerMinDistance)
				DroneMovement.MoveTo(lastKnownPlayerPosition, (float) Timing.DeltaTime);

			// if (PlayerInVisionRange(VisionAngleToAttack))
			// {
			// 	// Shoot the player
			// 	GD.Print("Shooting");
			// 	WeaponComponent.TryShooting();
			// }
		}
		else if (GlobalPosition.DistanceTo(lastKnownPlayerPosition) > 2f)
		{
			DroneMovement.MoveTo(lastKnownPlayerPosition, (float) Timing.DeltaTime);
		}
		else
		{
			GD.Print("Lost sight of player. Going back to patrol state.");
			currentState = EnemyState.Patrol;
		}
	}

	private IEnumerator<double> AttackCoroutine()
	{
		while (currentState is EnemyState.Attacking)
		{
			yield return Timing.WaitForOneFrame;

			if (HasLineOfSight())
			{
				lastKnownPlayerPosition = player.GlobalPosition;
				DroneMovement.MoveTo(lastKnownPlayerPosition, (float) Timing.DeltaTime);
			}
		}
	}
	
	#endregion
	
	private bool HasLineOfSight()
	{
		if (player is null)
			return false;

		return PlayerInRange() && PlayerInVisionRange(VisionAngle) && NoBuildingInBetween();
	}

	private bool PlayerInRange()
	{
		var distanceToPlayer = player.GlobalPosition.DistanceTo(GlobalPosition);
		return distanceToPlayer < VisionRange;
	}

	private bool PlayerInVisionRange(float visionRange)
	{
		var directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
		var forwardHorizontal = -GlobalTransform.Basis.Z with { Y = 0f };
		var angle = Mathf.RadToDeg(forwardHorizontal.AngleTo(directionToPlayer));
		return angle < visionRange / 2f;
	}

	private bool NoBuildingInBetween()
	{
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(
			from: GlobalPosition,
			to: player.GlobalPosition, 
			collisionMask: (uint) VisionMask,
			exclude: [GetRid()]);

		var result = spaceState.IntersectRay(query);
		return result.Count == 0;
	}
	
}
