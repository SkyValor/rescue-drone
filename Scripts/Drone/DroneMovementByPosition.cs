namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IDependent))]
public partial class DroneMovementByPosition : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    [Export] private float SpringStrength { get; set; } = 12f;
    [Export] private float Damping { get; set; } = 8f;
    [Export] private float MaxSpeed { get; set; } = 10f;

    [Dependency] private EnemyDrone Drone => this.DependOn<EnemyDrone>();
    
    private Vector3? targetPosition;
    
    public void SetTargetPosition(Vector3 targetPosition) => this.targetPosition = targetPosition;

    public void Tick(float deltaTime)
    {
        if (targetPosition is null)
            return;

        var target = targetPosition.Value;
        var direction = GlobalPosition.DirectionTo(target);
        var springForce = direction * SpringStrength;
        var dampingForce = -Drone.Velocity * Damping;
        var acceleration = springForce + dampingForce;
        Drone.Velocity += acceleration * deltaTime;

        if (Drone.Velocity.Length() > MaxSpeed)
            Drone.Velocity = Drone.Velocity.Normalized() * MaxSpeed;

        Drone.MoveAndSlide();
        RotateSmoothly(deltaTime);
    }

    public void MoveTo(Vector3 targetPosition, double deltaTime)
    {
        var delta = (float) deltaTime;
        var direction = targetPosition - GlobalPosition;
        var springForce = direction * SpringStrength;
        var dampingForce = -Drone.Velocity * Damping;
        var acceleration = springForce + dampingForce;
        Drone.Velocity += acceleration * delta;
        
        if (Drone.Velocity.Length() > MaxSpeed)
            Drone.Velocity = Drone.Velocity.Normalized() * MaxSpeed;
        
        Drone.MoveAndSlide();
        RotateSmoothly(delta);
    }

    public void MoveTowards(Node3D target, double deltaTime)
    {
        MoveBy(direction: target.GlobalPosition - GlobalPosition, (float) deltaTime);
    }

    public void MoveAwayFrom(Node3D target, double deltaTime)
    {
        MoveBy(direction: GlobalPosition - target.GlobalPosition, (float) deltaTime);
    }
    
    public void MoveBy(Vector3 direction, float deltaTime)
    {
        var springForce = direction * SpringStrength;
        var dampingForce = -Drone.Velocity * Damping;
        var acceleration = springForce + dampingForce;
        Drone.Velocity += acceleration * deltaTime;
        
        if (Drone.Velocity.Length() > MaxSpeed)
            Drone.Velocity = Drone.Velocity.Normalized() * MaxSpeed;
        
        Drone.MoveAndSlide();
        // RotateSmoothly(deltaTime);
    }

    public void RotateBy()
    {
        
    }

    public void LookAt(Node3D target, float deltaTime)
    {
        var forwardDirection = -GlobalTransform.Basis.Z;
        var targetDirection = target.GlobalPosition - GlobalPosition;

        var rotatedDirection = forwardDirection.MoveToward(targetDirection, 2f * deltaTime);

        var myBasis = Basis.Identity;
        
        // var targetBasis = Basis.Rotated(Vector3.Up, );
        //     
        // Basis.LookingAt(targetDirection, Vector3.Up);
        //
        // Drone.GlobalTransform = new Transform3D(
        //     Drone.Basis.Orthonormalized().Slerp())
    }

    private void RotateSmoothly(float delta)
    {
        var velocity = Drone.Velocity;
        if (velocity.Length() < 0.05f)
            return;

        var forward = velocity.Normalized() with { Y = 0f };
        var targetBasis = Basis.LookingAt(forward, Vector3.Up);
        targetBasis = targetBasis.Rotated(Vector3.Right, -velocity.Z * 0.02f);
        targetBasis = targetBasis.Rotated(Vector3.Forward, velocity.X * 0.02f);

        Drone.GlobalTransform = new Transform3D(
            Drone.GlobalTransform.Basis.Orthonormalized().Slerp(targetBasis, 3f * delta),
            GlobalPosition);
    }
    
}
