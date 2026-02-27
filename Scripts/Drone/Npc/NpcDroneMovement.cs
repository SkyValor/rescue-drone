namespace RescueDrone;

using System;
using System.Collections.Generic;
using Godot;
using MEC;

public partial class NpcDroneMovement : Node3D
{
    [Export] private float SpringStrength { get; set; } = 12f;		// How strongly it pulls
    [Export] private float Damping { get; set; } = 8f;				// How much it resists oscillation
    [Export] private float MaxSpeed { get; set; } = 10f;			// Clamp top speed

    [Export] private float OscillationMagnitude { get; set; } = 0.05f;
    [Export] private float OscillationHeight { get; set; } = 0.5f;
	
    [Export] private float AvoidanceStrength { get; set; } = 20f;
    [Export] private float AvoidanceDistance { get; set; } = 4f;

    private CharacterBody3D drone;
    private Vector3 targetPosition;
    private CoroutineHandle movementCoroutine;

    public override void _Ready()
    {
        drone = GetParentOrNull<Drone>();
        if (drone is null)
            throw new Exception($"Component of type {typeof(NpcDroneMovement)} requires a parent of type {typeof(Drone)}.");
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
        if (movementCoroutine.IsValid)
            Timing.KillCoroutines(movementCoroutine);

        movementCoroutine = Timing.RunCoroutine(FollowCoroutine().CancelWith(this), Segment.PhysicsProcess);
    }

    private IEnumerator<double> FollowCoroutine()
    {
        // TODO: Create a breaking condition
        while (true)
        {
            yield return Timing.WaitForOneFrame;
            var deltaTime = (float)Timing.DeltaTime;

            var directionToTarget = targetPosition - GlobalPosition;
            var springForce = directionToTarget * SpringStrength;
            var dampingForce = -drone.Velocity * Damping;
            //var avoidanceForce = GetAvoidanceForce();
            var acceleration = springForce * dampingForce;
            drone.Velocity += acceleration * deltaTime;
            
            // Clamp speed
            if (drone.Velocity.Length() > MaxSpeed)
                drone.Velocity = drone.Velocity.Normalized() * MaxSpeed;

            drone.MoveAndSlide();
            RotateSmoothly(deltaTime);
        }
    }

    private void RotateSmoothly(float deltaTime)
    {
        if (drone.Velocity.Length() < 0.05f)
            return;

        var forward = drone.Velocity.Normalized() with { Y = 0f };
        var targetBasis = Basis.LookingAt(forward, Vector3.Up);
        targetBasis = targetBasis.Rotated(Vector3.Right, -drone.Velocity.Z * 0.02f);
        targetBasis = targetBasis.Rotated(Vector3.Forward, drone.Velocity.X * 0.02f);

        drone.GlobalTransform = new Transform3D(
            drone.GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * deltaTime),
            drone.GlobalPosition);
    } 
    
}