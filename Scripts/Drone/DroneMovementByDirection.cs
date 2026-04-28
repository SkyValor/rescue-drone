namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using MEC;

[Meta(typeof(IDependent))]
public partial class DroneMovementByDirection : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] private float SpringForce { get; set; } = 12f;
    [Export] private float SpringDamping { get; set; } = 8f;
    [Export] private float MaxSpeed { get; set; } = 10f;
    
    [Export] private float RotationSpeed { get; set; } = 10f;

    [Dependency] private EnemyDrone Drone => this.DependOn<EnemyDrone>();
    
    private Drone drone;
    private Vector3 direction;

    private Node3D seekTarget;
    private float seekMinDistance;
    private bool isSeeking;
    private CoroutineHandle seekCoroutine;
    
    public void SetDrone(Drone drone) => this.drone = drone;
    public void SetDirection(Vector3 direction) => this.direction = direction;

    public void Tick(float deltaTime)
    {
        // if (direction == Vector3.Zero)
        //     return;
        //
        // var springForce = direction * SpringForce;
        // var dampingForce = -drone.Velocity * SpringDamping;
        // var acceleration = springForce + dampingForce;
        // drone.Velocity += acceleration * deltaTime;
        //
        // if (drone.Velocity.Length() > MaxSpeed)
        //     drone.Velocity = drone.Velocity.Normalized() * MaxSpeed;
        //
        // drone.MoveAndSlide();
        // RotateSmoothly(deltaTime);
    }

    public override void _ExitTree()
    {
        Timing.KillCoroutines(seekCoroutine);
    }

    public void SeekTarget(Node3D target, float minDistance = 0f)
    {
        Timing.KillCoroutines(seekCoroutine);
        
        seekTarget = target;
        seekMinDistance = minDistance;
        isSeeking = true;
        seekCoroutine = Timing.RunCoroutine(SeekCoroutine().CancelWith(this), Segment.PhysicsProcess);
    }

    public void StopSeeking()
    {
        Timing.KillCoroutines(seekCoroutine);
    }

    private IEnumerator<double> SeekCoroutine()
    {
        while (isSeeking && seekTarget is not null)
        {
            yield return Timing.WaitForOneFrame;
            
            var distanceToTarget = GlobalPosition.DistanceTo(seekTarget.GlobalPosition);
            if (distanceToTarget < seekMinDistance)
                continue;

            var targetDirection = GlobalPosition.DirectionTo(seekTarget.GlobalPosition);
            MoveToTarget(targetDirection, (float)Timing.DeltaTime);
        }
    }

    public void RotateToTarget(Vector3 targetRotation, float deltaTime)
    {
        drone.GlobalRotation = drone.GlobalRotation.Lerp(targetRotation, RotationSpeed * deltaTime);
    }
    
    private void MoveToTarget(Vector3 targetDirection, float deltaTime)
    {
        var springForce = targetDirection * SpringForce;
        var dampingForce = -Drone.Velocity * SpringDamping;
        var acceleration = springForce + dampingForce;
        Drone.Velocity += acceleration * deltaTime;

        if (Drone.Velocity.Length() > MaxSpeed)
            Drone.Velocity = Drone.Velocity.Normalized() * MaxSpeed;

        Drone.MoveAndSlide();
        RotateSmoothly(deltaTime);
    }
    
    private void RotateSmoothly(float deltaTime)
    {
        if (Drone.Velocity.Length() < 0.05f)
            return;

        var forward = Drone.Velocity.Normalized() with { Y = 0f };
        var targetBasis = Basis.LookingAt(forward, Vector3.Up);
        targetBasis = targetBasis.Rotated(Vector3.Right, -Drone.Velocity.Z * 0.02f);
        targetBasis = targetBasis.Rotated(Vector3.Forward, Drone.Velocity.X * 0.02f);

        Drone.GlobalTransform = new Transform3D(
            Drone.GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * deltaTime),
            GlobalPosition);
    }
    
}