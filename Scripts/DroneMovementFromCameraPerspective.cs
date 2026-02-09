namespace RescueDrone;

using Godot;

public partial class DroneMovementFromCameraPerspective : DroneMovement
{
    [Export] private Camera3D Camera { get; set; }
    
    // protected override void MoveDrone(float delta)
    // {
        // if (PitchIntent.IsZeroApprox() && CurrentPitchForce.IsZeroApprox() &&
        //     RollIntent.IsZeroApprox() && CurrentRollForce.IsZeroApprox())
        //     return;
        //
        // // Update force to be applied based on user's input
        // UpdateMotorForce(ref CurrentPitchForce, PitchIntent, AccelDelta, AccelFriction);
        // UpdateMotorForce(ref CurrentRollForce, RollIntent, RollSpeedDelta, RollSpeedFriction);
        //
        // // Get the force to be applied onto the velocity by sampling the graphical curve
        // var propulsionForceApplied = -AccelCurve.Sample(Mathf.Abs(CurrentPitchForce));
        // if (CurrentPitchForce > 0f)
        //     propulsionForceApplied *= -1f;
        //
        // var rollForceApplied = RollSpeedCurve.Sample(Mathf.Abs(CurrentRollForce));
        // if (CurrentRollForce < 0f)
        //     rollForceApplied *= -1f;
        //
        // // Get the direction relative to the drone
        // var droneRotation = Drone.GlobalTransform.Basis.GetEuler().Y;
        // var propulsionDirection = Vector3.Forward.Rotated(Vector3.Up, droneRotation).Normalized();
        // var propulsionVelocity = propulsionDirection * propulsionForceApplied * delta;
        //
        // var rollDirection = rollForceApplied > 0f
        //     ? Vector3.Right.Rotated(Vector3.Up, droneRotation).Normalized()
        //     : Vector3.Left.Rotated(Vector3.Up, droneRotation);
        // var rollVelocity = rollDirection * rollForceApplied * delta;
        //
        // // Move the drone
        // Drone.Velocity += propulsionVelocity + rollVelocity;
    // }
    
    private void UpdatePropulsionForce(float delta)
    {
        // if (!PitchIntent.IsZeroApprox())
        // {
        //     // User sends input to drone's propulsion. Use acceleration.
        //     CurrentPitchForce = Mathf.MoveToward(CurrentPitchForce, PitchIntent, AccelDelta * delta);
        //     if (CurrentPitchForce > 0f && PitchIntent < 0f)
        //         CurrentPitchForce = Mathf.MoveToward(CurrentPitchForce, PitchIntent, AccelDelta * delta);
        //     if (CurrentPitchForce < 0f && PitchIntent > 0f)
        //         CurrentPitchForce = Mathf.MoveToward(CurrentPitchForce, PitchIntent, AccelDelta * delta);
        // }
        // else
        // {
        //     // No input into the drone's propulsion. Decelerate using friction.
        //     CurrentPitchForce = Mathf.MoveToward(CurrentPitchForce, 0f, AccelFriction * delta);
        // }
    }

    private void UpdateStrafingForce(float delta)
    {
        // if (!RollIntent.IsZeroApprox())
        // {
        //     // User sends input to drone's strafing. Use acceleration.
        //     CurrentRollForce = Mathf.MoveToward(CurrentRollForce, RollIntent, RollSpeedDelta * delta);
        //     if (CurrentRollForce > 0f && RollIntent < 0f)
        //         CurrentRollForce = Mathf.MoveToward(CurrentRollForce, RollIntent, RollSpeedDelta * delta);
        //     if (CurrentRollForce < 0f && RollIntent > 0f)
        //         CurrentRollForce = Mathf.MoveToward(CurrentRollForce, RollIntent, RollSpeedDelta * delta);
        // }
        // else
        // {
        //     // No input into the drone's strafing. Decelerate using friction.
        //     CurrentRollForce = Mathf.MoveToward(CurrentRollForce, 0f, RollSpeedFriction * delta);
        // }
    }

    private Vector3 GetPropulsionVelocity(float camRotation, float propulsionSpeed, float delta)
    {
        // var propulsionDirection = new Vector3(x: 0f, y: 0f, z: CurrentPitchForce)
        //     .Rotated(Vector3.Up, camRotation)
        //     .Normalized();
        //
        // return propulsionDirection * propulsionSpeed * delta;
        return Vector3.Zero;
    }

    private Vector3 GetStrafingVelocity(float camRotation, float strafingSpeed, float delta)
    {
        // var strafingDirection = new Vector3(x: CurrentRollForce, y: 0f, z: 0f)
        //     .Rotated(Vector3.Up, camRotation)
        //     .Normalized();
        //
        // return strafingDirection * strafingSpeed * delta;
        return Vector3.Zero;
    }
    
}
