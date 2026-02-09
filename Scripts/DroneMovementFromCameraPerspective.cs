namespace RescueDrone;

using Godot;

public partial class DroneMovementFromCameraPerspective : DroneMovement
{
    [Export] private Camera3D Camera { get; set; }
    
    protected override void MoveDrone(float delta)
    {
        // There is no user input and the drone has no velocity. Nothing to do.
        if (PropulsionIntent.IsZeroApprox() && StrafingIntent.IsZeroApprox() && Drone.Velocity.IsZeroApprox())
            return;
        
        UpdatePropulsionForce(delta);
        UpdateStrafingForce(delta);
        
        // Move the drone in last velocity with a bit of deceleration
        // Drone.Velocity = Drone.Velocity.MoveToward(Vector3.Zero, 2f * delta);
        // Drone.MoveAndSlide();
        // return;
        
        var propulsionSpeed = -AccelCurve.Sample(Mathf.Abs(CurrentPropulsionForce)) * MaxPropulsionSpeed;
        var strafingSpeed = StrafingCurve.Sample(Mathf.Abs(CurrentStrafingForce)) * MaxStrafingSpeed;
        var camRotation = Camera.GlobalTransform.Basis.GetEuler().Y;
        
        var propulsionVelocity = GetPropulsionVelocity(camRotation, propulsionSpeed, delta);
        var strafingVelocity = GetStrafingVelocity(camRotation, strafingSpeed, delta);

        // Drone.Velocity = propulsionVelocity + strafingVelocity;
        // Drone.MoveAndSlide();

        // Reset the intent until next call, to avoid continuous force applied
        PropulsionIntent = 0f;
        StrafingIntent = 0f;
    }

    protected override void MoveDrone2(float delta)
    {
        if (InputVector.IsZeroApprox() && ForceVector.IsZeroApprox())
            return;

        UpdatePropulsionForceVector();
    }

    private void UpdatePropulsionForceVector()
    {
        var pitchIntent = InputVector.Z;
        var tiltIntent = InputVector.X;

        var pitchForce = ForceVector.Z;
        var tiltForce = ForceVector.X;

        switch (pitchIntent)
        {
            case 0f:
                pitchForce = Mathf.MoveToward(pitchForce, 0f, AccelFriction);
                break;
            case > 0f when pitchForce >= 0f:
                pitchForce = pitchForce < pitchIntent
                    ? Mathf.MoveToward(pitchForce, pitchIntent, AccelWeight)
                    : Mathf.MoveToward(pitchForce, pitchIntent, AccelFriction);
                break;
            case > 0f:
                pitchForce = Mathf.MoveToward(pitchForce, pitchIntent, AccelWeight * 2f);
                break;
            case < 0f when pitchForce <= 0f:
                pitchForce = pitchForce > pitchIntent
                    ? Mathf.MoveToward(pitchForce, pitchIntent, AccelWeight)
                    : Mathf.MoveToward(pitchForce, pitchIntent, AccelFriction);
                break;
            case < 0f:
                pitchForce = Mathf.MoveToward(pitchForce, pitchIntent, AccelWeight * 2f);
                break;
        }
        
        
    }
    

    private void UpdateForceVectorByIntent()
    {
        
    }

    private void UpdatePropulsionForceByIntent()
    {
        if (PropulsionIntent.IsZeroApprox() && !CurrentPropulsionForce.IsZeroApprox())
        {
            // Without user input into propulsion, decrease the force by friction.
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, 0f, AccelFriction);
        }
        else if (PropulsionIntent > 0f)
        {
            if (CurrentPropulsionForce < 0f)
                CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * 2f);
            else
            {
            }
            
            // if (CurrentPropulsionForce >= 0f)
                // CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent);
        }
        else
        {
            
        }
    }

    private Vector3 VelocityFromPropulsion(float camRotation, float delta)
    {
        UpdatePropulsionForceByIntent();
        
        
        
        UpdatePropulsionForce(delta);
        var propulsionSpeed = -AccelCurve.Sample(Mathf.Abs(CurrentPropulsionForce)) * MaxPropulsionSpeed;
        
        if (!PropulsionIntent.IsZeroApprox())
        {
            // When the user intends to move forward or back, use camera rotation for the velocity.
            var propulsionVelocity = GetPropulsionVelocity(camRotation, propulsionSpeed, delta);
        }
        else
        {
            // Otherwise, let the drone drift towards last velocity.
        }

        return Vector3.Zero;
    }
    
    private void UpdatePropulsionForce(float delta)
    {
        if (!PropulsionIntent.IsZeroApprox())
        {
            // User sends input to drone's propulsion. Use acceleration.
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
            if (CurrentPropulsionForce > 0f && PropulsionIntent < 0f)
                CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
            if (CurrentPropulsionForce < 0f && PropulsionIntent > 0f)
                CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, PropulsionIntent, AccelWeight * delta);
        }
        else
        {
            // No input into the drone's propulsion. Decelerate using friction.
            CurrentPropulsionForce = Mathf.MoveToward(CurrentPropulsionForce, 0f, AccelFriction * delta);
        }
    }

    private void UpdateStrafingForce(float delta)
    {
        if (!StrafingIntent.IsZeroApprox())
        {
            // User sends input to drone's strafing. Use acceleration.
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
            if (CurrentStrafingForce > 0f && StrafingIntent < 0f)
                CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
            if (CurrentStrafingForce < 0f && StrafingIntent > 0f)
                CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, StrafingIntent, StrafingWeight * delta);
        }
        else
        {
            // No input into the drone's strafing. Decelerate using friction.
            CurrentStrafingForce = Mathf.MoveToward(CurrentStrafingForce, 0f, StrafingFriction * delta);
        }
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
