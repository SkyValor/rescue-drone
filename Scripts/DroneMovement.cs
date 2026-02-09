namespace RescueDrone;

using Godot;

public partial class DroneMovement : Node
{
    [ExportGroup("Propulsion")]
    [Export] protected Curve AccelCurve { get; set; }
    [Export] protected float MaxPropulsionSpeed { get; set; } = 200f;
    [Export] protected float AccelWeight { get; set; } = 0.5f;
    [Export] protected float AccelFriction { get; set; } = 0.75f;
    [ExportGroup("Strafing")]
    [Export] protected Curve StrafingCurve { get; set; }
    [Export] protected float MaxStrafingSpeed { get; set; } = 80f;
    [Export] protected float StrafingWeight { get; set; } = 0.5f;
    [Export] protected float StrafingFriction { get; set; } = 0.75f;
    [ExportGroup("Rotation")]
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
    
    protected float PropulsionIntent;
    protected float CurrentPropulsionForce;
    protected float StrafingIntent;
    protected float CurrentStrafingForce;
    protected float TurningIntent;

    protected Vector3 CalculatedVelocity;
    
    public void SetDrone(Drone drone) => Drone = drone;

    public void SetPropulsionIntent(float intent) => PropulsionIntent = intent;
    public void SetStrafingIntent(float intent) => StrafingIntent = intent;
    public void SetTurningIntent(float intent) => TurningIntent = intent;

    public void Tick(float delta)
    {
        CalculatedVelocity = Vector3.Zero;
        ThrottleDrone(delta);
        RotateDrone(delta);
        MoveDrone(delta);
    }

    private void ThrottleDrone(float delta)
    {
        if (ThrottleIntent.IsZeroApprox() && CurrentThrottleForce.IsZeroApprox())
            return;

        switch (ThrottleIntent)
        {
            case 0f:
                // When there is no input, take from the current force by friction
                CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, 0f, ThrottleFriction);
                break;
            case > 0f when CurrentThrottleForce >= 0f:
                // If the current force is less than the intent, add to it by delta;
                // Otherwise, take from it by friction
                CurrentThrottleForce = CurrentThrottleForce < ThrottleIntent
                    ? Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta)
                    : Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleFriction);
                break;
            case > 0f:
                // When the input is to the opposite direction, add to the current force by twice the delta
                CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta * 2f);
                break;
            case < 0f when CurrentThrottleForce <= 0f:
                // If current force is greater than the intent, add to it by delta;
                // Otherwise, take from it by friction
                CurrentThrottleForce = CurrentThrottleForce > ThrottleIntent
                    ? Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta)
                    : Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleFriction);
                break;
            case < 0f:
                // When the input is to the opposite direction, add to the current force by twice the delta
                CurrentThrottleForce = Mathf.MoveToward(CurrentThrottleForce, ThrottleIntent, ThrottleDelta * 2f);
                break;
        }
        
        var throttleSpeed = ThrottleCurve.Sample(Mathf.Abs(CurrentThrottleForce));
        var throttleVelocity = Vector3.Up * throttleSpeed * delta;
        CalculatedVelocity += throttleVelocity;
    }

    private void RotateDrone(float delta)
    {
        if (TurningIntent == 0f)
            return;
        
        // Rotate the drone towards the intent
        var rotation = Drone.Rotation;
        var targetRotation = TurningIntent < 0f ? rotation.Y - 50f : rotation.Y + 50f;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetRotation, RotationSpeed * delta);
        Drone.Rotation = rotation;
        
        // Reset the intent until next call, to avoid continuous turning applied
        TurningIntent = 0f;
    }

    protected virtual void MoveDrone(float delta) { }
    
}
