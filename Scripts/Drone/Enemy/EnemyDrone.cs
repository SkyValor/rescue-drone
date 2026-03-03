namespace RescueDrone;

using Chickensoft.GodotNodeInterfaces;
using Godot;
using Godot.Collections;

public interface IEnemyDrone : ICharacterBody3D;

public partial class EnemyDrone : CharacterBody3D, IEnemyDrone
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
	[Export] private float PatrolLookoutAngle { get; set; } = 45f;
	[Export] private float SearchDuration { get; set; }

	private EnemyLogic EnemyStateMachine { get; set; }
	private EnemyLogic.IBinding EnemyBinding { get; set; }

	private EnemyState state = EnemyState.Idle;
	private Waypoint currentWaypoint;
	private Waypoint previousWaypoint;
	private Drone player;
	private Vector3 lastKnownPlayerPosition;
	private float searchTimer;

	public override void _Ready()
	{
		EnemyStateMachine = new EnemyLogic();
		EnemyStateMachine.Set(this as IEnemyDrone);
		EnemyStateMachine.Set(Waypoints);
		EnemyStateMachine.Set(new EnemyLogic.Settings(SpringStrength, Damping, MaxSpeed));

		EnemyBinding = EnemyStateMachine.Bind();
		EnemyBinding.Handle((in EnemyLogic.Output.VelocityChanged output) =>
			Velocity = output.Velocity);
		
		EnemyStateMachine.Start();
	}

	public override void _ExitTree()
	{
		EnemyStateMachine.Stop();
		EnemyBinding.Dispose();
	}

	public override void _PhysicsProcess(double delta)
	{
		EnemyStateMachine.Input(new EnemyLogic.Input.PhysicsTick(delta));

		MoveAndSlide();
		RotateSmoothly(delta);
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
	
	// TODO: Make HasLineOfSight its own coroutine and be running while enemy is not Attacking.
	// When the enemy detects the player, break out of this coroutine and change the state. This will break out of any other coroutines.

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
