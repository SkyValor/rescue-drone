namespace RescueDrone;

using Godot;

public partial class DroneRotationHandler : Node
{
    [Export] private float PitchMaxDegrees { get; set; }
    [Export] private float PitchDelta { get; set; } = 1f;
    
    [Export] private float RollMaxDegrees { get; set; }
    [Export] private float RollDelta { get; set; } = 1f;
    
    private Drone drone;
    private float inputPitch;
    private float currentPitch;
    private float inputRoll;
    private float currentRoll;
    private float yaw;
    private float throttle;

    public void SetDrone(Drone drone) => this.drone = drone;
    
    public void SetPitch(float pitch) => inputPitch = pitch;
    public void SetRoll(float roll) => inputRoll = roll;
    public void SetYaw(float yaw) => this.yaw = yaw;
    public void SetThrottle(float throttle) => this.throttle = throttle;

    // TODO: Node3D PlayerLookAtTarget must not be a child of PlayerDrone
    // because when the PlayerDrone rotates in X-axis during pitch, it goes up or down
    // and the PhantomCamera will rotate as well.
    //
    // Make the PlayerLookAtTarget separate from PlayerDrone, but react to its transformation,
    // such as when it moves in World Space, and move according to it.
    
    public void Tick(float delta)
    {
        UpdateRotationForce(ref currentPitch, inputPitch, PitchDelta);
        currentPitch = Mathf.MoveToward(currentPitch, inputPitch, PitchDelta * delta);
        
        UpdateRotationForce(ref currentRoll, inputRoll, RollDelta);
        currentRoll = Mathf.MoveToward(currentRoll, inputRoll, RollDelta * delta);

        drone.RotationDegrees = drone.RotationDegrees with
        {
            // X = -currentPitch * PitchMaxDegrees,
            Z = -currentRoll * RollMaxDegrees
        };
        
        // drone.RotationDegrees += new Vector3(x: -currentPitch * PitchMaxDegrees, 0f, -currentRoll * RollMaxDegrees);
    }

    private static void UpdateRotationForce(ref float currentForce, float inputForce, float forceDelta)
    {
        if ((currentForce < 0f && inputForce > 0f) || (currentForce > 0f && inputForce < 0f))
            currentForce = Mathf.MoveToward(currentForce, inputForce, forceDelta * 2f);
        else
            currentForce = Mathf.MoveToward(currentForce, inputForce, forceDelta);
    }
    
}
