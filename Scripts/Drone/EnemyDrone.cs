namespace RescueDrone;

using System;
using Godot;
using Godot.Collections;

public partial class EnemyDrone : CharacterBody3D
{
	public enum EnemyState
	{
		Idle,
		Patrol,
		Attacking,
		Searching
	}

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
	[Export] private int VisionMask { get; set; }
	[Export] private float SearchDuration { get; set; }

	private EnemyState state = EnemyState.Idle;
	private Waypoint currentWaypoint;
	private Waypoint previousWaypoint;
	private Drone player;
	private Vector3 lastKnownPlayerPosition;
	private float searchTimer;

	public override void _PhysicsProcess(double delta)
	{
		var deltaTime = (float)delta;
		switch (state)
		{
			case EnemyState.Idle:
				ProcessIdle();
				break;
			case EnemyState.Patrol:
				ProcessPatrol(deltaTime);
				break;
			case EnemyState.Attacking:
				break;
			case EnemyState.Searching:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void ProcessIdle()
	{
		state = HasLineOfSight() ? EnemyState.Attacking : EnemyState.Patrol;
	}

	private void ProcessPatrol(float deltaTime)
	{
		if (HasLineOfSight())
		{
			GD.Print("Line of sight discovers player. Engaging...");
			state = EnemyState.Attacking;
			return;
		}

		currentWaypoint ??= GetClosestWaypoint();
		if (currentWaypoint is null)
		{
			GD.PrintErr("Enemy drone cannot get closest waypoint.");
			return;
		}
		
		var distanceToWaypoint = GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
		if (distanceToWaypoint < 0.25f)
		{
			var nextWaypoint = GetNextWaypoint();
			previousWaypoint = currentWaypoint;
			currentWaypoint = nextWaypoint;
		}
		
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

	private bool HasLineOfSight()
	{
		if (player is null)
			return false;

		var toPlayer = player.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() > VisionRange)
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

		var collider = result["collider"];
		GD.Print("What is collider...");
		return false;
	}

	private void RotateSmoothly(float deltaTime)
	{
		if (Velocity.Length() < 0.05f)
			return;

		var forward = Velocity.Normalized() with { Y = 0f };
		var targetBasis = Basis.LookingAt(forward, Vector3.Up);
		targetBasis = targetBasis.Rotated(Vector3.Right, -Velocity.Z * 0.02f);
		targetBasis = targetBasis.Rotated(Vector3.Forward, Velocity.X * 0.02f);
		
		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * deltaTime),
			GlobalPosition);
	}
	
}
