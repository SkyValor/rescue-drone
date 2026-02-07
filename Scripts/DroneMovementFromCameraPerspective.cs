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
        
        var rotation = Drone.Rotation;
        var targetRotation = TurningIntent < 0f ? rotation.Y - 50f : rotation.Y + 50f;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetRotation, RotationSpeed * delta);
        Drone.Rotation = rotation;
    }
    
    protected override void MoveDrone(float delta)
    {
        base.MoveDrone(delta);
        
        // Update propulsion force by acceleration/deceleration
        CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        if (CurrentPropulsionForce > 0f && PropulsionIntent < 0f)
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        if (CurrentPropulsionForce < 0f && PropulsionIntent > 0f)
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        
        // Get the current speed from the Curve
        var propulsionSpeed = -AccelCurve.Sample(Mathf.Abs(CurrentPropulsionForce)) * MaxPropulsionSpeed;
        
        // Update strafing force by acceleration/deceleration
        CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        if (CurrentStrafingForce > 0f && StrafingIntent < 0f)
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        if (CurrentStrafingForce < 0f && StrafingIntent > 0f)
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        
        // Get the current speed for side travel from the Curve
        var strafingSpeed = StrafingCurve.Sample(Mathf.Abs(CurrentStrafingForce)) * MaxStrafingSpeed;
        
        // Get the camera's Rotation to calculate movement direction
        var camRotation = Camera.GlobalTransform.Basis.GetEuler().Y;
        
        // Get move front/back direction based on camera view
        var propulsionDirection = new Vector3(x: 0f, y: 0f, z: CurrentPropulsionForce)
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        var propulsionVelocity = propulsionDirection * propulsionSpeed * delta;
        
        // Get the left/right direction based on camera view
        var strafingDirection = new Vector3(x: CurrentStrafingForce, y: 0f, z: 0f)
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        var strafingVelocity = strafingDirection * strafingSpeed * delta;

        // Do the movement
        Drone.Velocity = propulsionVelocity + strafingVelocity;
        Drone.MoveAndSlide();

        // Reset the intent until next call, to avoid continuous force applied
        PropulsionIntent = 0f;
        StrafingIntent = 0f;
        TurningIntent = 0f;
    }
    
}
