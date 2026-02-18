using System.Collections.Generic;
using Godot;
using MEC;

namespace RescueDrone;

public partial class SmallDrone : CharacterBody3D
{
	[Export] private Drone Player { get; set; }
	[Export] private Vector3 FollowOffset = new(2, 0, 0);		// Offset to keep the drone at player's side

	[Export] private float SpringStrength { get; set; } = 12f;		// How strongly it pulls
	[Export] private float Damping { get; set; } = 8f;				// How much it resists oscillation
	[Export] private float MaxSpeed { get; set; } = 10f;			// Clamp top speed

	[Export] private float OscillationMagnitude { get; set; } = 0.05f;
	[Export] private float OscillationHeight { get; set; } = 0.5f;

	private bool isFollowing;
	private CoroutineHandle? followCoroutine;

	public void StartFollowing(Drone player)
	{
		if (followCoroutine is not null)
			Timing.KillCoroutines((CoroutineHandle)followCoroutine);
		
		Player = player;
		isFollowing = true;
		followCoroutine = Timing.RunCoroutine(FollowCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}

	public void StopFollowing()
	{
		Player = null;
		isFollowing = false;
		Velocity = Vector3.Zero;
		if (followCoroutine is null)
			return;
		
		Timing.KillCoroutines((CoroutineHandle)followCoroutine);
		followCoroutine = null;
	}

	private IEnumerator<double> FollowCoroutine()
	{
		while (isFollowing && Player is not null)
		{
			yield return Timing.WaitForOneFrame;
			
			var deltaTime = (float)Timing.DeltaTime;
			
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
			RotateSmoothly(deltaTime);
		}
	}

	private void RotateSmoothly(float deltaTime)
	{
		if (Velocity.Length() < 0.05f)
			return;

		var forward = Velocity.Normalized();
		forward = forward with { Y = 0f };
		var targetBasis = Basis.LookingAt(forward, Vector3.Up);
		targetBasis = targetBasis.Rotated(Vector3.Right, -Velocity.Z * 0.02f);
		targetBasis = targetBasis.Rotated(Vector3.Forward, Velocity.X * 0.02f);

		GlobalTransform = new Transform3D(
			GlobalTransform.Basis.Slerp(targetBasis, 3f * deltaTime),
			GlobalTransform.Origin);
	}
	
}
