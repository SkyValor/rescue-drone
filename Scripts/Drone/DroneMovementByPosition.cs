namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IDependent))]
public partial class DroneMovementByPosition : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] private float SpringForce { get; set; } = 12f;
    [Export] private float SpringDamping { get; set; } = 8f;
    [Export] private float MaxSpeed { get; set; } = 10f;

    [Dependency] private Drone Drone => this.DependOn<Drone>();
    
    private Vector3? targetPosition;
    
    public void SetTargetPosition(Vector3 targetPosition) => this.targetPosition = targetPosition;

    public void Tick(float deltaTime)
    {
        if (targetPosition is null)
            return;

        var target = targetPosition.Value;
        var direction = GlobalPosition.DirectionTo(target);
        var springForce = direction * SpringForce;
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