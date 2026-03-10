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
	DroneMovementByDirection DroneMovement { get; set; }
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
	
	private enum LookoutDirection { Left, Right }
	
	#region Exports

	[ExportGroup("Drone Movement Stats")]
	[Export] private float SpringStrength { get; set; } = 12f;		// How strongly it pulls
	[Export] private float Damping { get; set; } = 8f;				// How much it resists oscillation
	[Export] private float MaxSpeed { get; set; } = 10f;			// Clamp top speed

	[Export] private float OscillationMagnitude { get; set; } = 0.05f;
	[Export] private float OscillationHeight { get; set; } = 0.5f;
	
	[Export] private float AvoidanceStrength { get; set; } = 20f;
	[Export] private float AvoidanceDistance { get; set; } = 4f;
	
	[ExportGroup("Drone Movement Waypoints")]
	[Export] private Array<Waypoint> Waypoints { get; set; }
	[Export] private float VisionRange { get; set; }
	[Export] private float VisionAngle { get; set; } = 30f;
	
	[Export] private int VisionMask { get; set; }
	[Export] private float LookoutAngle { get; set; } = 45f;
	[Export] private float LookoutDuration { get; set; }

	#endregion
	
	[Node] public DroneMovementByDirection DroneMovement { get; set; }

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
	private CoroutineHandle lookoutCoroutine;
	private CoroutineHandle moveToWaypointCoroutine;
	
	public override void _Ready()
	{
		// EnemyStateMachine = new EnemyLogic();
		player = GetTree().GetNodesInGroup("player")[0] as Drone;
		this.Provide();
		//
		// EnemyStateMachine.Set(this as IEnemyDrone);
		// EnemyStateMachine.Set(Waypoints);
		// EnemyStateMachine.Set(DroneMovement);
		// EnemyStateMachine.Set(new EnemyLogic.Settings(LookoutDuration: 4f, LookoutAngle: 35f));
		//
		// EnemyBinding = EnemyStateMachine.Bind();
		// EnemyBinding
		// 	.Handle((in EnemyLogic.Output.VelocityChanged output) =>
		// 		Velocity = output.Velocity)
		// 	.Handle((in EnemyLogic.Output.RotationRequest output) =>
		// 		DroneMovement.RotateToTarget(output.TargetRotation, output.Delta));
			
		//EnemyStateMachine.Start();
	}

	public override void _ExitTree()
	{
		EnemyStateMachine.Stop();
		EnemyBinding.Dispose();
	}

	public override void _PhysicsProcess(double delta)
	{
		//EnemyStateMachine.Input(new EnemyLogic.Input.PhysicsTick((float)delta));

		switch (currentState)
		{
			case EnemyState.Idle:
				ProcessIdle();
				break;
			case EnemyState.Patrol:
				ProcessPatrol((float)delta);
				break;
		}

		//MoveAndSlide();
		//RotateSmoothly(delta);
	}

	private void ProcessIdle()
	{
		currentState = HasLineOfSight() ? EnemyState.Attacking : EnemyState.Patrol;
	}

	private void ProcessPatrol(float deltaTime)
	{
		if (HasLineOfSight())
		{
			GD.Print("Line of sight discovers player. Engaging...");
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
			yield return Timing.WaitForOneFrame;

			var deltaTime = (float)Timing.DeltaTime;
			var targetPosition = currentWaypoint.GlobalPosition;
			var direction = targetPosition - GlobalPosition;

			var springForce = direction * SpringStrength;
			var dampingForce = -Velocity * Damping;
			//var avoidanceForce = GetAvoidanceForce();
			var acceleration = springForce + dampingForce;
			Velocity += acceleration * deltaTime;
		
			// Clamp speed
			if (Velocity.Length() > MaxSpeed)
				Velocity = Velocity.Normalized() * MaxSpeed;

			MoveAndSlide();
			RotateSmoothly(deltaTime);
		}

		movingToWaypoint = false;
	}
	
	private void InitiateLookout()
	{
		isOnLookout = true;
		lookoutCoroutine = Timing.RunCoroutine(LookoutCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}

	private void InterruptLookout()
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
	
	private bool HasLineOfSight()
	{
		if (player is null)
			return false;

		var distanceToPlayer = player.GlobalPosition.DistanceTo(GlobalPosition);
		if (distanceToPlayer > VisionRange)
			return false;


		var playerVec = (GlobalPosition - player.GlobalPosition).Normalized();
		var forward = GlobalTransform.Basis.Z;
		var dot = forward.Dot(playerVec);
		GD.Print(dot);
		
		var angleToPlayer = forward.AngleTo(player.GlobalPosition);
		if (angleToPlayer > VisionAngle)
			return false;
		
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(
			from: GlobalPosition,
			to: player.GlobalPosition, 
			collisionMask: (uint) VisionMask,
			exclude: [GetRid()]);

		var result = spaceState.IntersectRay(query);
		if (result.Count == 0)
			return false;

		// var collider = result["collider"];
		// if (collider.Obj is Drone) GD.Print("Player detected");
		
		return false;
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

	private void GoToNextWaypoint()
	{
		var nextWaypoint = GetNextWaypoint();
		previousWaypoint = currentWaypoint;
		currentWaypoint = nextWaypoint;
		MoveToWaypoint();
	}
	
	private Waypoint GetNextWaypoint()
	{
		var connections = currentWaypoint.Connections.Duplicate();
		if (previousWaypoint is not null)
			connections.Remove(previousWaypoint);

		return connections.Count == 0 ? previousWaypoint : connections.PickRandom();
	}

	private void RotateSmoothly(double deltaTime)
	{
		if (Velocity.Length() < 0.05f)
			return;

		var forward = Velocity.Normalized() with { Y = 0f };
		var targetBasis = Basis.LookingAt(forward, Vector3.Up);
		targetBasis = targetBasis.Rotated(Vector3.Right, -Velocity.Z * 0.02f);
		targetBasis = targetBasis.Rotated(Vector3.Forward, Velocity.X * 0.02f);
		
		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * (float)deltaTime),
			GlobalPosition);
	}
	
}
