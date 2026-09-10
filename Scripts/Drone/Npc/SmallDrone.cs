namespace RescueDrone;

using System.Collections.Generic;
using Godot;
using MEC;

public partial class SmallDrone : CharacterBody3D
{
	[Export] private float SpringStrength { get; set; } = 12f;		// How strongly it pulls
	[Export] private float Damping { get; set; } = 8f;				// How much it resists oscillation
	[Export] private float MaxSpeed { get; set; } = 10f;			// Clamp top speed

	[Export] private float OscillationMagnitude { get; set; } = 0.05f;
	[Export] private float OscillationHeight { get; set; } = 0.5f;
	
	[Export] private float AvoidanceStrength { get; set; } = 20f;
	[Export] private float AvoidanceDistance { get; set; } = 4f;

	private RayCast3D rayForward;
	private RayCast3D rayLeft;
	private RayCast3D rayRight;
	private RayCast3D[] rays;

	private DroneFormation formation;
	private int formationIndex;
	private bool isFollowing;
	private float oscillationModifier;
	private CoroutineHandle? followCoroutine;

	public override void _Ready()
	{
		var raycastNames = new[]
		{
			"RayForward", "RayForwardLeft", "RayForwardRight", 
			"RayLeft", "RayRight", 
			"RayBack", "RayBackLeft", "RayBackRight"
		};
		rays = new RayCast3D[raycastNames.Length];
		for (int index = 0; index < raycastNames.Length; index++)
		{
			rays[index] = GetNode<RayCast3D>(raycastNames[index]);
		}
	}

	public void SetFormation(DroneFormation formation, int formationIndex)
	{
		if (followCoroutine is not null)
			Timing.KillCoroutines((CoroutineHandle)followCoroutine);
		
		this.formation = formation;
		this.formationIndex = formationIndex;
		isFollowing = true;
		oscillationModifier = GD.Randf();
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
			var timePassed = Time.GetTicksMsec() / 1000f;
			targetPosition.Y += Mathf.Sin(timePassed + oscillationModifier * OscillationMagnitude * deltaTime) * OscillationHeight;
			var direction = targetPosition - GlobalTransform.Origin;
		
			var springForce = direction * SpringStrength;
			var dampingForce = -Velocity * Damping;
			var avoidanceForce = GetAvoidanceForce();
			var acceleration = springForce + dampingForce + avoidanceForce;
			Velocity += acceleration * deltaTime;

			// Clamp speed
			if (Velocity.Length() > MaxSpeed)
				Velocity = Velocity.Normalized() * MaxSpeed;

			MoveAndSlide();
			RotateSmoothly(deltaTime);
		}
	}

	private Vector3 GetAvoidanceForce()
	{
		var force = Vector3.Zero;
		foreach (var ray in rays)
		{
			if (!ray.IsColliding())
				continue;

			var hitPoint = ray.GetCollisionPoint();
			var hitNormal = ray.GetCollisionNormal();
			var distance = GlobalPosition.DistanceTo(hitPoint);
			var strength = 1f - (distance / AvoidanceDistance);
			strength = Mathf.Clamp(strength, 0f, 1f);
			force += hitNormal * strength * AvoidanceStrength;
		}

		return force;
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
			GlobalTransform.Origin);
	}
	
}
