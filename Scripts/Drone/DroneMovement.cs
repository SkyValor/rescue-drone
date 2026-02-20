namespace RescueDrone;

using Godot;

/// <summary>
/// Ensures the target <see cref="drone"/> moves according to input set in matching fields.
/// For each kind of movement being requested, applies some force of velocity in a respective direction
/// relative to the drone.
/// <para/>
/// Features <see cref="Curve"/>s to provide dynamic acceleration/deceleration with different values
/// of delta and friction to be used in speed calculation. <c>Delta</c> fields are used for acceleration, while
/// <c>friction</c> fields are used for deceleration.
/// </summary>
public partial class DroneMovement : Node
{
    [ExportGroup("Pitch Speed")]
    [Export] protected Curve PitchCurve { get; set; }
    [Export] protected float PitchDelta { get; set; } = 0.5f;
    [Export] protected float PitchFriction { get; set; } = 0.75f;
    [ExportGroup("Roll Speed")]
    [Export] protected Curve RollSpeedCurve { get; set; }
    [Export] protected float RollSpeedDelta { get; set; } = 0.5f;
    [Export] protected float RollSpeedFriction { get; set; } = 0.75f;
    [ExportGroup("Throttle Speed")]
    [Export] protected Curve ThrottleCurve { get; set; }
    [Export] protected float ThrottleDelta { get; set; } = 0.5f;
    [Export] protected float ThrottleFriction { get; set; } = 0.75f;
    [ExportGroup("Yaw Turning Speed")]
    [Export] protected Curve YawCurve { get; set; }
    [Export] protected float YawDelta { get; set; } = 0.5f;
    [Export] protected float YawFriction { get; set; } = 0.75f;

    private Drone drone;
    
    private float pitchIntent;
    private float currentPitchForce;
    
    private float rollIntent;
    private float currentRollForce;

    private float yawIntent;
    private float currentYawForce;
    
    private float throttleIntent;
    private float currentThrottleForce;
    
    public void SetDrone(Drone drone) => this.drone = drone;

    public void SetPitchIntent(float intent) => pitchIntent = intent * PitchCurve.MaxDomain;
    public void SetRollIntent(float intent) => rollIntent = intent * RollSpeedCurve.MaxDomain;
    public void SetYawIntent(float intent) => yawIntent = intent * YawCurve.MaxDomain;
    public void SetThrottleIntent(float intent) => throttleIntent = intent * ThrottleCurve.MaxDomain;

    public void Tick(float delta)
    {
        var droneRotation = drone.GlobalTransform.Basis.GetEuler();
        drone.Velocity = Vector3.Zero;
        AddThrottleVelocity(delta);
        TurnDrone(delta);
        AddPitchVelocity(delta, droneRotation);
        AddRollVelocity(delta, droneRotation);
        drone.MoveAndSlide();
    }

    private void AddThrottleVelocity(float delta)
    {
        if (throttleIntent.IsZeroApprox() && currentThrottleForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref currentThrottleForce, throttleIntent, ThrottleDelta, ThrottleFriction);

        var forceApplied = ThrottleCurve.Sample(Mathf.Abs(currentThrottleForce));
        forceApplied *= currentThrottleForce > 0f ? 1f : -1f;
        
        drone.Velocity += Vector3.Up * forceApplied * delta;
    }

    private void TurnDrone(float delta)
    {
        if (yawIntent.IsZeroApprox() && currentYawForce.IsZeroApprox())
            return;

        UpdateMotorForce(ref currentYawForce, yawIntent, YawDelta, YawFriction);
        var forceApplied = YawCurve.Sample(Mathf.Abs(currentYawForce));
        
        // We are changing the drone's rotation in Euler angles,
        // therefore we set a negative value to rotate rightward
        if (currentYawForce > 0f) 
            forceApplied *= -1f;

        drone.RotationDegrees += new Vector3(0f, forceApplied * delta, 0f);
    }

    private void AddPitchVelocity(float delta, Vector3 droneRotation)
    {
        if (pitchIntent.IsZeroApprox() && currentPitchForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref currentPitchForce, pitchIntent, PitchDelta, PitchFriction);
        
        var pitchForceApplied = -PitchCurve.Sample(Mathf.Abs(currentPitchForce));
        pitchForceApplied *= currentPitchForce > 0f ? -1f : 1f; // Invert because Forward is negative Z-axis
        
        var pitchDirection = Vector3.Forward.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        drone.Velocity += pitchDirection * pitchForceApplied * delta;
    }

    private void AddRollVelocity(float delta, Vector3 droneRotation)
    {
        if (rollIntent.IsZeroApprox() && currentRollForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref currentRollForce, rollIntent, RollSpeedDelta, RollSpeedFriction);
        
        var rollForceApplied = RollSpeedCurve.Sample(Mathf.Abs(currentRollForce));
        var rollDirection = currentRollForce > 0f
            ? Vector3.Right.Rotated(Vector3.Up, droneRotation.Y).Normalized()
            : Vector3.Left.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        drone.Velocity += rollDirection * rollForceApplied * delta;
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
