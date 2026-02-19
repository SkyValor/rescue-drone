namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using MEC;

public partial class SmallDrone : CharacterBody3D
{
	[Export] private Drone Player { get; set; }

	[Export] private float SpringStrength { get; set; } = 12f;		// How strongly it pulls
	[Export] private float Damping { get; set; } = 8f;				// How much it resists oscillation
	[Export] private float MaxSpeed { get; set; } = 10f;			// Clamp top speed

	[Export] private float OscillationMagnitude { get; set; } = 0.05f;
	[Export] private float OscillationHeight { get; set; } = 0.5f;

	private DroneFormation formation;
	private int formationIndex;
	private bool isFollowing;
	private CoroutineHandle? followCoroutine;

	public void SetFormation(DroneFormation formation, int formationIndex)
	{
		if (followCoroutine is not null)
			Timing.KillCoroutines((CoroutineHandle)followCoroutine);
		
		this.formation = formation;
		this.formationIndex = formationIndex;
		isFollowing = true;
		followCoroutine = Timing.RunCoroutine(FollowCoroutine().CancelWith(this), Segment.PhysicsProcess);
	}

	private IEnumerator<double> FollowCoroutine()
	{
		while (isFollowing)
		{
			yield return Timing.WaitForOneFrame;
			
			var deltaTime = (float)Timing.DeltaTime;
			
			// Calculate desired world position with offset and subtle vertical motion
			var targetPosition = formation.GetSlotPosition(formationIndex);
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
			GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * deltaTime),
			GlobalTransform.Origin);
	}
	
}
