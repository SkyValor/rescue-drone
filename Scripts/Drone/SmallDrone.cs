using Godot;

namespace RescueDrone;

public partial class SmallDrone : CharacterBody3D
{
	[Export] public Drone Player { get; set; }
	[Export] public float FollowSpeed { get; set; }
	[Export] public float Acceleration { get; set; }
	[Export] public float StopDistance { get; set; }

	// Offset to keep the drone at player's side
	[Export] public Vector3 FollowOffset = new Vector3(2, 0, 0);

	private bool isFollowing;

	public void StartFollowing(Drone player)
	{
		GD.Print("SmallDrone start following player!");
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
		
		// Calculate desired world position with offset
		var desiredPosition = Player.GlobalTransform.Origin + Player.GlobalTransform.Basis * FollowOffset;
		var direction = desiredPosition - GlobalTransform.Origin;
		var distance = direction.Length();
		if (distance > StopDistance)
		{
			direction = direction.Normalized();
			var targetVelocity = direction * FollowSpeed;
			Velocity = Velocity.Lerp(targetVelocity, Acceleration * deltaTime);
		}
		else
		{
			// Slow down smoothly when close enough
			Velocity = Velocity.Lerp(Vector3.Zero, Acceleration * deltaTime);
		}

		MoveAndSlide();
		RotateTowardsMovement(deltaTime);
	}

	private void RotateTowardsMovement(float deltaTime)
	{
		if (Velocity.Length() < 0.1f)
			return;

		var lookDirection = Velocity.Normalized();
		var targetBasis = Basis.LookingAt(lookDirection, Vector3.Up);
		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Slerp(targetBasis, 5f * deltaTime),
			GlobalTransform.Origin);
	}
	
}
