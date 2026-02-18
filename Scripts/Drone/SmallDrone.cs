using Godot;

namespace RescueDrone;

public partial class SmallDrone : CharacterBody3D
{
	[Export] public Drone Player { get; set; }
	[Export] public float FollowSpeed { get; set; }
	[Export] public float Acceleration { get; set; }
	[Export] public float StopDistance { get; set; }

	[Export] public Vector3 FollowOffset = new(2, 0, 0);		// Offset to keep the drone at player's side

	[Export] public float SpringStrength { get; set; } = 12f;		// How strongly it pulls
	[Export] public float Damping { get; set; } = 8f;				// How much it resists oscillation
	[Export] public float MaxSpeed { get; set; } = 10f;				// Clamp top speed

	[Export] public float OscillationMagnitude { get; set; } = 0.05f;
	[Export] public float OscillationHeight { get; set; } = 0.5f;

	private bool isFollowing;

	public void StartFollowing(Drone player)
	{
		Player = player;
		isFollowing = true;
	}

	public void StopFollowing()
	{
		Player = null;
		isFollowing = false;
		Velocity = Vector3.Zero;
	}

	// TODO: Convert into coroutine...
	public override void _PhysicsProcess(double delta)
	{
		if (!isFollowing || Player is null)
			return;

		var deltaTime = (float)delta;
		
		// Calculate desired world position with offset and subtle vertical motion
		var targetPosition = Player.GlobalTransform.Origin + Player.GlobalTransform.Basis * FollowOffset;
		targetPosition.Y += Mathf.Sin(Time.GetTicksMsec() * OscillationMagnitude * deltaTime) * OscillationHeight;
		var direction = targetPosition - GlobalTransform.Origin;
		
		var springForce = direction * SpringStrength;
		var dampingForce = -Velocity * Damping;
		var acceleration = springForce + dampingForce;
		Velocity += acceleration * deltaTime;

		// Clamp speed
		if (Velocity.Length() > MaxSpeed)
			Velocity = Velocity.Normalized() * MaxSpeed;

		MoveAndSlide();
		RotateTowardsMovement(deltaTime);
	}
	
	private void RotateTowardsMovement(float deltaTime)
	{
		// TODO: Rotation should not include the X-axis.
		// Or, don't rotate on throttle movement.
		
		if (Velocity.Length() < 0.1f)
			return;

		var lookDirection = Velocity.Normalized();
		lookDirection = lookDirection with { Y = 0f };
		var targetBasis = Basis.LookingAt(lookDirection, Vector3.Up);
		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Slerp(targetBasis, 5f * deltaTime),
			GlobalTransform.Origin);
	}

	private void RotateSmoothly(float deltaTime)
	{
		if (Velocity.Length() < 0.05f)
			return;

		var forward = Velocity.Normalized();
		var targetBasis = Basis.LookingAt(forward, Vector3.Up);
		targetBasis = targetBasis.Rotated(Vector3.Right, -Velocity.Z * 0.02f);
		targetBasis = targetBasis.Rotated(Vector3.Forward, Velocity.X * 0.02f);

		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Slerp(targetBasis, 3f * deltaTime),
			GlobalTransform.Origin);
	}
	
}
