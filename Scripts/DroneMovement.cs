namespace RescueDrone;

using Godot;

public partial class DroneMovement : Node
{
    [ExportGroup("Pitch")]
    [Export] protected Curve AccelCurve { get; set; }
    [Export] protected float MaxPropulsionSpeed { get; set; } = 200f;
    [Export] protected float AccelWeight { get; set; } = 0.5f;
    [Export] protected float AccelFriction { get; set; } = 0.75f;
    [ExportGroup("Tilt")]
    [Export] protected Curve StrafingCurve { get; set; }
    [Export] protected float MaxStrafingSpeed { get; set; } = 80f;
    [Export] protected float StrafingWeight { get; set; } = 0.5f;
    [Export] protected float StrafingFriction { get; set; } = 0.75f;
    [ExportGroup("Rotation")]
    [Export] protected Curve RotationCurve { get; set; }
    [Export] protected float RotationDelta { get; set; } = 0.5f;
    [Export] protected float RotationFriction { get; set; } = 0.75f;
    [Export] protected float RotationSpeed { get; set; } = 50f;
    [ExportGroup("Throttle")]
    [Export] protected Curve ThrottleCurve { get; set; }
    [Export] protected float ThrottleDelta { get; set; } = 0.5f;
    [Export] protected float ThrottleFriction { get; set; } = 0.75f;

    protected Drone Drone;
    protected Vector3 InputVector;
    protected Vector3 ForceVector;

    protected float ThrottleIntent;
    protected float CurrentThrottleForce;
    protected float CurrentThrottleCurveX;

    protected float RotationIntent;
    protected float CurrentRotationForce;
    
    protected float PropulsionIntent;
    protected float CurrentPropulsionForce;
    protected float StrafingIntent;
    protected float CurrentStrafingForce;
    protected float TurningIntent;

    public void SetDrone(Drone drone) => Drone = drone;

    public void SetPropulsionIntent(float intent) => PropulsionIntent = intent;
    public void SetStrafingIntent(float intent) => StrafingIntent = intent;
    public void SetTurningIntent(float intent) => RotationIntent = intent * RotationCurve.MaxDomain;
    public void SetThrottleIntent(float intent) => ThrottleIntent = intent * ThrottleCurve.MaxDomain;

    public void Tick(float delta)
    {
        Drone.Velocity = Vector3.Zero;
        ThrottleDrone(delta);
        RotateDrone(delta);
        // MoveDrone(delta);
        // Drone.Velocity = throttleVelocity;
        Drone.MoveAndSlide();
    }

    // private void ThrottleDrone(float delta)
    // {
    //     if (ThrottleIntent.IsZeroApprox() && CurrentThrottleForce.IsZeroApprox())
    //         return;
    //     
    //     switch (ThrottleIntent)
    //     {
    //         case 0f:
    //             // When there is no input, take from the current force by friction
    //             CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, 0f, ThrottleFriction);
    //             break;
    //         case > 0f when CurrentThrottleForce >= 0f:
    //             // If the current force is less than the intent, add to it by delta;
    //             // Otherwise, take from it by friction
    //             CurrentThrottleForce = CurrentThrottleForce < ThrottleIntent
    //                 ? Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta)
    //                 : Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleFriction);
    //             break;
    //         case > 0f:
    //             // When the input is to the opposite direction, add to the current force by twice the delta
    //             CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta * 2f);
    //             break;
    //         case < 0f when CurrentThrottleForce <= 0f:
    //             // If current force is greater than the intent, add to it by delta;
    //             // Otherwise, take from it by friction
    //             CurrentThrottleForce = CurrentThrottleForce > ThrottleIntent
    //                 ? Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta)
    //                 : Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleFriction);
    //             break;
    //         case < 0f:
    //             // When the input is to the opposite direction, add to the current force by twice the delta
    //             CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta * 2f);
    //             break;
    //     }
    //     
    //     var curveX = CurrentThrottleForce * ThrottleCurve.MaxDomain;
    //     var throttleSpeed = ThrottleCurve.Sample(Mathf.Abs(curveX));
    //     throttleSpeed += CurrentThrottleForce > 0f ? 1 : -1;
    //     
    //     var throttleVelocity = Vector3.Up * throttleSpeed * delta;
    //     CalculatedVelocity += throttleVelocity;
    // }

    private void ThrottleDrone(float delta)
    {
        if (ThrottleIntent.IsZeroApprox() && CurrentThrottleForce.IsZeroApprox())
            return;
        
        UpdateMotorForce(ref CurrentThrottleForce, ThrottleIntent, ThrottleDelta, ThrottleFriction);

        var forceApplied = ThrottleCurve.Sample(Mathf.Abs(CurrentThrottleForce));
        forceApplied *= CurrentThrottleForce > 0f ? 1f : -1f;
        GD.Print("Force applied: " + forceApplied);
        
        Drone.Velocity += Vector3.Up * forceApplied * delta;
    }

    private static void UpdateMotorForce(ref float currentForce, float intent, float accelDelta, float friction)
    {
        switch (intent)
        {
            case 0f:
                currentForce = Mathf.MoveToward(currentForce, 0f, friction);
                GD.Print("No input. Current force: " + currentForce);
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

    private void RotateDrone(float delta)
    {
        if (RotationIntent.IsZeroApprox() && CurrentRotationForce.IsZeroApprox())
            return;

        UpdateMotorForce(ref CurrentRotationForce, RotationIntent, RotationDelta, RotationFriction);
        var forceApplied = RotationCurve.Sample(Mathf.Abs(CurrentRotationForce));
        
        // We are changing the drone's rotation in Euler angles,
        // therefore we set a negative value to rotate rightward
        if (CurrentRotationForce > 0f) 
            forceApplied *= -1f;
        
        var rotation = Drone.Rotation;
        rotation.Y += forceApplied * delta;
        Drone.Rotation = rotation;
        
        // Reset the intent until next call, to avoid continuous turning applied
        TurningIntent = 0f;
    }

    protected virtual void MoveDrone(float delta) { }
    
    protected virtual void MoveDrone2(float delta) { }
    
}
