namespace RescueDrone;

using Godot;

public partial class DroneMovement : Node
{
    [ExportGroup("Pitch")]
    [Export] protected Curve AccelCurve { get; set; }
    [Export] protected float AccelDelta { get; set; } = 0.5f;
    [Export] protected float AccelFriction { get; set; } = 0.75f;
    [ExportGroup("Roll")]
    [Export] protected Curve RollSpeedCurve { get; set; }
    [Export] protected float RollSpeedDelta { get; set; } = 0.5f;
    [Export] protected float RollSpeedFriction { get; set; } = 0.75f;
    [ExportGroup("Yaw")]
    [Export] protected Curve RotationCurve { get; set; }
    [Export] protected float RotationDelta { get; set; } = 0.5f;
    [Export] protected float RotationFriction { get; set; } = 0.75f;
    [ExportGroup("Throttle")]
    [Export] protected Curve ThrottleCurve { get; set; }
    [Export] protected float ThrottleDelta { get; set; } = 0.5f;
    [Export] protected float ThrottleFriction { get; set; } = 0.75f;

    private Drone Drone;
    
    private float PitchIntent;
    private float CurrentPitchForce;
    
    private float RollIntent;
    private float CurrentRollForce;

    private float YawIntent;
    private float CurrentYawForce;
    
    private float ThrottleIntent;
    private float CurrentThrottleForce;
    
    public void SetDrone(Drone drone) => Drone = drone;

    public void SetPitchIntent(float intent) => PitchIntent = intent * AccelCurve.MaxDomain;
    public void SetRollIntent(float intent) => RollIntent = intent * RollSpeedCurve.MaxDomain;
    public void SetYawIntent(float intent) => YawIntent = intent * RotationCurve.MaxDomain;
    public void SetThrottleIntent(float intent) => ThrottleIntent = intent * ThrottleCurve.MaxDomain;

    public void Tick(float delta)
    {
        var droneRotation = Drone.GlobalTransform.Basis.GetEuler();
        Drone.Velocity = Vector3.Zero;
        AddThrottleVelocity(delta);
        TurnDrone(delta);
        AddPitchVelocity(delta, droneRotation);
        AddRollVelocity(delta, droneRotation);
        Drone.MoveAndSlide();
    }

    private void AddThrottleVelocity(float delta)
    {
        if (ThrottleIntent.IsZeroApprox() && CurrentThrottleForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref CurrentThrottleForce, ThrottleIntent, ThrottleDelta, ThrottleFriction);

        var forceApplied = ThrottleCurve.Sample(Mathf.Abs(CurrentThrottleForce));
        forceApplied *= CurrentThrottleForce > 0f ? 1f : -1f;
        
        Drone.Velocity += Vector3.Up * forceApplied * delta;
    }

    private void TurnDrone(float delta)
    {
        if (YawIntent.IsZeroApprox() && CurrentYawForce.IsZeroApprox())
            return;

        UpdateMotorForce(ref CurrentYawForce, YawIntent, RotationDelta, RotationFriction);
        var forceApplied = RotationCurve.Sample(Mathf.Abs(CurrentYawForce));
        
        // We are changing the drone's rotation in Euler angles,
        // therefore we set a negative value to rotate rightward
        if (CurrentYawForce > 0f) 
            forceApplied *= -1f;
        
        var rotation = Drone.Rotation;
        rotation.Y += forceApplied * delta;
        Drone.Rotation = rotation;
    }

    private void AddPitchVelocity(float delta, Vector3 droneRotation)
    {
        if (PitchIntent.IsZeroApprox() && CurrentPitchForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref CurrentPitchForce, PitchIntent, AccelDelta, AccelFriction);
        
        var pitchForceApplied = -AccelCurve.Sample(Mathf.Abs(CurrentPitchForce));
        pitchForceApplied *= CurrentPitchForce > 0f ? -1f : 1f; // Invert because Forward is negative Z-axis
        
        var pitchDirection = Vector3.Forward.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        Drone.Velocity += pitchDirection * pitchForceApplied * delta;
    }

    private void AddRollVelocity(float delta, Vector3 droneRotation)
    {
        if (RollIntent.IsZeroApprox() && CurrentRollForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref CurrentRollForce, RollIntent, RollSpeedDelta, RollSpeedFriction);
        
        var rollForceApplied = RollSpeedCurve.Sample(Mathf.Abs(CurrentRollForce));
        var rollDirection = CurrentRollForce > 0f
            ? Vector3.Right.Rotated(Vector3.Up, droneRotation.Y).Normalized()
            : Vector3.Left.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        Drone.Velocity += rollDirection * rollForceApplied * delta;
    }
    
    private static void UpdateMotorForce(ref float currentForce, float intent, float accelDelta, float friction)
    {
        switch (intent)
        {
            case 0f:
                currentForce = Mathf.MoveToward(currentForce, 0f, friction);
                break;
            case > 0f when currentForce >= 0f:
                currentForce = currentForce < intent
                    ? Mathf.MoveToward(currentForce, intent, accelDelta)
                    : Mathf.MoveToward(currentForce, intent, friction);
                break;
            case > 0f:
                currentForce = Mathf.MoveToward(currentForce, intent, accelDelta + friction);
                break;
            case < 0f when currentForce <= 0f:
                currentForce = currentForce > intent
                    ? Mathf.MoveToward(currentForce, intent, accelDelta)
                    : Mathf.MoveToward(currentForce, intent, friction);
                break;
            case < 0f:
                currentForce = Mathf.MoveToward(currentForce, intent, accelDelta + friction);
                break;
        }
    }
    
}
