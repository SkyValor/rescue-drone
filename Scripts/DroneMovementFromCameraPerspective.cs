namespace RescueDrone;

using Godot;

public partial class DroneMovementFromCameraPerspective : DroneMovement
{
    [Export] private Camera3D Camera { get; set; }

    protected override void RotateDrone(float delta)
    {
        base.RotateDrone(delta);
        
        // Rotate the ship towards the intent
        if (TurningIntent == 0f)
            return;
        
        // TODO: This part is not working properly yet...
        
        var rotation = Drone.Rotation;
        var targetRotation = TurningIntent < 0f ? rotation.Y - 50f : rotation.Y + 50f;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetRotation, RotationSpeed * delta);
        Drone.Rotation = rotation;
        
        // Reset the intent until next call, to avoid continuous force applied
        TurningIntent = 0f;
    }
    
    protected override void MoveDrone(float delta)
    {
        base.MoveDrone(delta);

        if (PropulsionIntent == 0f && StrafingIntent == 0f && Drone.Velocity != Vector3.Zero)
        {
            // Move the drone in last velocity with a bit of deceleration.
            GD.Print("Inside: " + Time.GetTicksMsec());
            Drone.Velocity = Drone.Velocity.MoveToward(Vector3.Zero, 2f * delta);
            Drone.MoveAndSlide();
            return;
        }
        
        GD.Print("Outside: " + Time.GetTicksMsec());
        
        UpdatePropulsionForce(delta);
        UpdateStrafingForce(delta);
        
        var propulsionSpeed = -AccelCurve.Sample(Mathf.Abs(CurrentPropulsionForce)) * MaxPropulsionSpeed;
        var strafingSpeed = StrafingCurve.Sample(Mathf.Abs(CurrentStrafingForce)) * MaxStrafingSpeed;
        var camRotation = Camera.GlobalTransform.Basis.GetEuler().Y;
        
        var propulsionVelocity = GetPropulsionVelocity(camRotation, propulsionSpeed, delta);
        var strafingVelocity = GetStrafingVelocity(camRotation, strafingSpeed, delta);

        Drone.Velocity = propulsionVelocity + strafingVelocity;
        Drone.MoveAndSlide();

        // Reset the intent until next call, to avoid continuous force applied
        PropulsionIntent = 0f;
        StrafingIntent = 0f;
    }

    private void UpdatePropulsionForce(float delta)
    {
        CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        if (CurrentPropulsionForce > 0f && PropulsionIntent < 0f)
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        if (CurrentPropulsionForce < 0f && PropulsionIntent > 0f)
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
    }

    private void UpdateStrafingForce(float delta)
    {
        CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        if (CurrentStrafingForce > 0f && StrafingIntent < 0f)
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        if (CurrentStrafingForce < 0f && StrafingIntent > 0f)
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
    }

    private Vector3 GetPropulsionVelocity(float camRotation, float propulsionSpeed, float delta)
    {
        var propulsionDirection = new Vector3(x: 0f, y: 0f, z: CurrentPropulsionForce)
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        
        return propulsionDirection * propulsionSpeed * delta;
    }

    private Vector3 GetStrafingVelocity(float camRotation, float strafingSpeed, float delta)
    {
        var strafingDirection = new Vector3(x: CurrentStrafingForce, y: 0f, z: 0f)
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        
        return strafingDirection * strafingSpeed * delta;
    }
    
}
