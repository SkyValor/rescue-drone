namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class DroneModel : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public float MaxTiltAngleDegrees { get; private set; } = 25f;
    [Export] public float TiltLerpSpeed { get; private set; } = 6.0f;
    [Export] public float HoverBobFrequency { get; private set; } = 2.0f;
    [Export] public float HoverBobAmplitude { get; private set; } = 0.05f;
    
    [Dependency] private PlayerMover Drone => this.DependOn<PlayerMover>();
    
    private Node3D DroneMesh { get; set; }
    
    public void OnPhysicsProcess(double delta)
    {
        ApplyVisualTilt((float) delta);
        ApplyHoverBob();
    }

    private void ApplyVisualTilt(float deltaTime)
    {
        var currentHorizontalVelocity = Drone.Velocity with { Y = 0f };
        var maxHorizontalSpeed = Drone.MaxHorizontalSpeed;
        
        // Convert global velocity into the drone's local coordinate space
        var localVelocity = Transform.Basis.Inverse() * currentHorizontalVelocity;

        // Calculate target tilt angles proportional to current local speed
        // Moving Forward (-Z local) tilts the nose Down (Negative X rotation)
        // Moving Right (+X local) tilts the body Left (Negative Z rotation)
        var targetPitch = (localVelocity.Z / maxHorizontalSpeed) * Mathf.DegToRad(MaxTiltAngleDegrees);
        var targetRoll = -(localVelocity.X / maxHorizontalSpeed) * Mathf.DegToRad(MaxTiltAngleDegrees);

        // Smoothly interpolate current visual rotations toward targets
        var currentRotation = DroneMesh.Rotation;
        currentRotation.X = Mathf.LerpAngle(currentRotation.X, targetPitch, TiltLerpSpeed * deltaTime);
        currentRotation.Z = Mathf.LerpAngle(currentRotation.Z, targetRoll, TiltLerpSpeed * deltaTime);
        
        DroneMesh.Rotation = currentRotation;
    }
    
    private void ApplyHoverBob()
    {
        // 1.0 is the STOPPING SPEED
        if (Drone.Velocity.Length() < 1f)
        {
            var bobOffset = Mathf.Sin(Time.GetTicksMsec() * 0.001f * HoverBobFrequency) * HoverBobAmplitude;
            var meshPosition = DroneMesh.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, bobOffset, 0.1f);
            DroneMesh.Position = meshPosition;
        }
        else
        {
            // Return to local origin smoothly when actively flying
            var meshPosition = DroneMesh.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, 0.0f, 0.1f);
            DroneMesh.Position = meshPosition;
        }
    }
}
