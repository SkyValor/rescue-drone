namespace RescueDrone;

using System;
using Godot;
using PhantomCamera;

public partial class PlayerMover : CharacterBody3D
{
    public enum ControlType { Type1, Type2 }
    
    private const float MOUSE_SENSITIVITY = 0.001f;
    
    [ExportGroup("Speed Settings")]
    [Export] public float MaxHorizontalSpeed = 15.0f;
    [Export] public float MaxVerticalSpeed = 10.0f;

    [ExportGroup("Momentum Settings")]
    [Export] public float Acceleration = 20.0f;
    [Export] public float Deceleration = 12.0f;
    [Export] public float RotationSpeed = 3.0f;
    
    [ExportGroup("Juice & Visuals")]
    [Export] public Node3D DroneMesh; 
    [Export] public float MaxTiltAngleDegrees = 25.0f;
    [Export] public float TiltLerpSpeed = 6.0f;
    [Export] public float HoverBobFrequency = 2.0f;
    [Export] public float HoverBobAmplitude = 0.05f;
    
    [ExportGroup("Camera Settings")]
    [Export] public ControlType Control { get; private set; } = ControlType.Type1;
    [Export] public float CamRotationSpeed = 3.0f;

    private PhantomCamera3D pCam;
    private float target_rotation_x;
    private float target_rotation_y;
    
    public override void _Ready()
    {
        if (DroneMesh is null && HasNode("DroneMesh"))
            DroneMesh = GetNode<Node3D>("DroneMesh");

        pCam = GetNode<Node3D>("../PlayerThirdPersonCamera").AsPhantomCamera3D();
        if (Control is ControlType.Type1 && pCam.FollowMode == FollowMode3D.ThirdPerson)
        {
            Input.SetMouseMode(Input.MouseModeEnum.Captured);
            SetProcessUnhandledInput(true);
            return;
        }
        
        SetProcessUnhandledInput(false);
    }
    
    #region Physics

    public override void _UnhandledInput(InputEvent @event)
    {
        // Rotate the camera around the player by the motion of mouse
        if (@event is not InputEventMouseMotion mouseMotion) return;
        
        var currentRotation = pCam.GetThirdPersonRotation();
        currentRotation.Y -= mouseMotion.Relative.X * MOUSE_SENSITIVITY;
        currentRotation.X -= mouseMotion.Relative.Y * MOUSE_SENSITIVITY;
        // Clamp pitch to prevent the camera from flipping over
        currentRotation.X = Mathf.Clamp(currentRotation.X, Mathf.DegToRad(-80), Mathf.DegToRad(80));
        
        pCam.SetThirdPersonRotation(currentRotation);
    }

    public override void _PhysicsProcess(double delta)
    {
        var deltaTime = (float) delta;
        switch (Control)
        {
            case ControlType.Type1:
                PhysicsProcessWithType1(deltaTime);
                break;
            case ControlType.Type2:
                PhysicsProcessWithType2(deltaTime);
                break;
            default:
                throw new NotImplementedException("Anything besides type 1 or 2 not implemented.");
        }
    }

    /// <summary>
    /// In Type-1, the user rotates the third-person-camera around the drone using the mouse.
    /// This is handled inside _UnhandledInput().
    ///
    /// Furthermore, the drone will always rotate to match the looking direction
    /// of the camera. Other controls are done with keyboard keys.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void PhysicsProcessWithType1(float deltaTime)
    {
        if (pCam is null) return;

        var camDirection = GetCamDirectionalsFlattened();
        AlignDroneNoseWithCamera(camDirection.Forward, deltaTime);
        
        var inputDirection = GetHorizontalInput();
        var verticalInput = GetVerticalInput();

        // Calculate horizontal direction based on the drone's current rotation basis
        var horizontalDirection = (camDirection.Right * inputDirection.X) - (camDirection.Forward * inputDirection.Y);
        if (horizontalDirection.LengthSquared() > 1.0f)
            horizontalDirection = horizontalDirection.Normalized();
        
        var currentVelocity = Velocity;
        var horizontalVelocity = ProcessHorizontalVelocity(horizontalDirection, currentVelocity, deltaTime);
        var verticalVelocity = ProcessVerticalVelocity(verticalInput, currentVelocity, deltaTime);
    
        Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
        MoveAndSlide();
        
        if (DroneMesh == null) return;
        
        ApplyVisualTilt(horizontalVelocity, deltaTime);
        ApplyHoverBob(inputDirection, verticalInput);
    }
    
    /// <summary>
    /// In Type-2, the user rotates the third-person-camera around the drone with arrow keys.
    /// Other controls are done with keyboard keys.
    ///
    /// Furthermore, the drone will always rotate to match the looking direction
    /// of the camera.
    /// </summary>
    /// <param name="deltaTime"></param>
    private void PhysicsProcessWithType2(float deltaTime)
    {
        if (pCam is null) return;
        
        RotateCameraWithKeys(deltaTime);

        var camForward = -pCam.Node3D.GlobalTransform.Basis.Z;
        var camRight = pCam.Node3D.GlobalTransform.Basis.X;
        
        FlattenToXZ(ref camForward);
        FlattenToXZ(ref camRight);
        
        AlignDroneNoseWithCamera(camForward, deltaTime);
        
        var inputDirection = GetHorizontalInput();
        var verticalInput = GetVerticalInput();
        var direction = GetDirectionRelativeToCamera(inputDirection);

        // Rotate the drone's nose to face the direction it is traveling.
        // if (direction.LengthSquared() > 0.01f)
        //     RotateRelativeToCamera(direction, deltaTime);

        var currentVelocity = Velocity;
        var horizontalVelocity = ProcessHorizontalVelocity(direction, currentVelocity, deltaTime);
        var verticalVelocity = ProcessVerticalVelocity(verticalInput, currentVelocity, deltaTime);

        Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
        MoveAndSlide();

        if (DroneMesh is null) return;
        
        ApplyVisualTilt(horizontalVelocity, deltaTime);
        ApplyHoverBob(inputDirection, verticalInput);
    }
    
    // TODO:
    // In type 3, we are still missing the camera always mimicking the drone's rotation.
    
    private void PhysicsProcessWithType3(float deltaTime)
    {
        HandleYawRotation(deltaTime);
        
        var inputDirection = GetHorizontalInput();
        var verticalInput = GetVerticalInput();
        
        // Calculate horizontal direction based on the drone's current rotation basis
        var direction = (Transform.Basis.X * inputDirection.X) + (Transform.Basis.Z * inputDirection.Y);
        if (direction.LengthSquared() > 1.0f)
            direction = direction.Normalized();
        
        var currentVelocity = Velocity;
        var horizontalVelocity = ProcessHorizontalVelocity(direction, currentVelocity, deltaTime);
        var verticalVelocity = ProcessVerticalVelocity(verticalInput, currentVelocity, deltaTime);
    
        Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
        MoveAndSlide();
        
        if (DroneMesh == null) return;
        
        ApplyVisualTilt(horizontalVelocity, deltaTime);
        ApplyHoverBob(inputDirection, verticalInput);
    }
    
    #endregion

    private static Vector2 GetHorizontalInput() => 
        Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    
    private static float GetRotationInput() => Input.GetAxis("turn_right", "turn_left");
    
    private static float GetVerticalInput() => Input.GetAxis("throttle_down", "throttle_up");

    private (Vector3 Forward, Vector3 Right) GetCamDirectionalsFlattened()
    {
        var camForward = -pCam.Node3D.GlobalTransform.Basis.Z;
        var camRight = pCam.Node3D.GlobalTransform.Basis.X;

        // Flatten vectors onto the horizontal plane (X/Z) so camera pitch doesn't tilt physics forces
        camForward.Y = 0;
        camRight.Y = 0;
        camForward = camForward.Normalized();
        camRight = camRight.Normalized();
        
        return (camForward, camRight);
    }
    
    /// <summary>
    /// Calculate horizontal and vertical velocity in order to have a smooth lerp towards
    /// those values. Commit that and call <see cref="CharacterBody3D.MoveAndSlide()"/>.
    /// </summary>
    /// <param name="horizontalDirection"></param>
    /// <param name="verticalInput"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    private Vector3 CommitMovement(Vector3 horizontalDirection, float verticalInput, float deltaTime)
    {
        var currentVelocity = Velocity;
        var horizontalVelocity = ProcessHorizontalVelocity(horizontalDirection, currentVelocity, deltaTime);
        var verticalVelocity = ProcessVerticalVelocity(verticalInput, currentVelocity, deltaTime);

        Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
        MoveAndSlide();

        return horizontalVelocity;
    }
    
    private Vector3 ProcessHorizontalVelocity(Vector3 direction, Vector3 currentVelocity, float deltaTime)
    {
        var targetHorizontalVelocity = direction * MaxHorizontalSpeed;
        var currentHorizontalVelocity = new Vector3(currentVelocity.X, 0, currentVelocity.Z);
        var horizontalForce = direction.LengthSquared() > 0 ? Acceleration : Deceleration;
        return currentHorizontalVelocity.MoveToward(targetHorizontalVelocity, horizontalForce * deltaTime);
    }

    private float ProcessVerticalVelocity(float verticalInput, Vector3 currentVelocity, float deltaTime)
    {
        var targetVerticalVelocity = verticalInput * MaxVerticalSpeed;
        var currentVerticalStep = Mathf.Abs(verticalInput) > 0 ? Acceleration : Deceleration;
        return Mathf.MoveToward(currentVelocity.Y, targetVerticalVelocity, currentVerticalStep * deltaTime);
    }

    private void HandleYawRotation(float deltaTime)
    {
        var rotationInput = GetRotationInput();
        RotateY(rotationInput * RotationSpeed * deltaTime);
    }
    
    // Arrow keys to rotate the camera this way
    private void RotateCameraWithKeys(float deltaTime)
    {
        var currentRotation = pCam.GetThirdPersonRotation();
            
        // Get button inputs for turning
        if (Input.IsActionPressed("cam_rotate_up"))
            target_rotation_x += CamRotationSpeed * deltaTime;
        if (Input.IsActionPressed("cam_rotate_down"))
            target_rotation_x -= CamRotationSpeed * deltaTime;
        
        if (Input.IsActionPressed("cam_rotate_left"))
            target_rotation_y += CamRotationSpeed * deltaTime;
        if (Input.IsActionPressed("cam_rotate_right"))
            target_rotation_y -= CamRotationSpeed * deltaTime;
		
        // Smoothly interpolate the rotation
        currentRotation.X = Mathf.LerpAngle(currentRotation.X, target_rotation_x, CamRotationSpeed * deltaTime);
        currentRotation.Y = Mathf.LerpAngle(currentRotation.Y, target_rotation_y, CamRotationSpeed * deltaTime);
        pCam.SetThirdPersonRotation(currentRotation);
    }

    private Vector3 GetDirectionRelativeToCamera(Vector2 inputDirection)
    {
        Vector3 direction;
        if (pCam != null)
        {
            // Get camera directional vectors
            var camForward = pCam.Node3D.GlobalTransform.Basis.Z;
            var camRight = pCam.Node3D.GlobalTransform.Basis.X;

            // Flatten vectors onto the horizontal plane (X/Z) so camera pitch doesn't affect speed
            camForward.Y = 0;
            camRight.Y = 0;
            camForward = camForward.Normalized();
            camRight = camRight.Normalized();

            // Combine camera perspective with player input
            direction = (camRight * inputDirection.X) + (camForward * inputDirection.Y);
        }
        else
        {
            // Fallback to global space if no camera is present
            direction = new Vector3(inputDirection.X, 0, inputDirection.Y);
        }

        if (direction.LengthSquared() > 1.0f)
            direction = direction.Normalized();

        return direction;
    }

    private static void FlattenToXZ(ref Vector3 vector)
    {
        vector.Y = 0;
        vector = vector.Normalized();
    }

    private void AlignDroneNoseWithCamera(Vector3 camForward, float deltaTime)
    {
        // Atan2 calculates the angle targeting the exact horizon direction the camera points
        var targetLookAngle = Mathf.Atan2(-camForward.X, -camForward.Z);
        var rotation = Rotation;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetLookAngle, RotationSpeed * deltaTime);
        Rotation = rotation;
    }

    private void RotateRelativeToCamera(Vector3 direction, float deltaTime)
    {
        // Calculates the angle on the Y-axis towards the movement direction
        var targetAngle = Mathf.Atan2(-direction.X, -direction.Z);
            
        var rotation = Rotation;
        rotation.Y = Mathf.LerpAngle(rotation.Y, targetAngle, RotationSpeed * deltaTime);
        Rotation = rotation;
    }
    
    private void ApplyVisualTilt(Vector3 currentHorizontalVelocity, float deltaTime)
    {
        // Convert global velocity into the drone's local coordinate space
        var localVelocity = Transform.Basis.Inverse() * currentHorizontalVelocity;

        // Calculate target tilt angles proportional to current local speed
        // Moving Forward (-Z local) tilts the nose Down (Negative X rotation)
        // Moving Right (+X local) tilts the body Left (Negative Z rotation)
        var targetPitch = (localVelocity.Z / MaxHorizontalSpeed) * Mathf.DegToRad(MaxTiltAngleDegrees);
        var targetRoll = -(localVelocity.X / MaxHorizontalSpeed) * Mathf.DegToRad(MaxTiltAngleDegrees);

        // Smoothly interpolate current visual rotations toward targets
        var currentRotation = DroneMesh.Rotation;
        currentRotation.X = Mathf.LerpAngle(currentRotation.X, targetPitch, TiltLerpSpeed * deltaTime);
        currentRotation.Z = Mathf.LerpAngle(currentRotation.Z, targetRoll, TiltLerpSpeed * deltaTime);
        
        DroneMesh.Rotation = currentRotation;
    }
    
    private void ApplyHoverBob(Vector2 inputDirection, float verticalInput)
    {
        // Only apply a subtle idle bob up and down if the drone is relatively stationary
        if (inputDirection.LengthSquared() == 0 && Mathf.Abs(verticalInput) < 0.1f)
        {
            var bobOffset = Mathf.Sin(Time.GetTicksMsec() * 0.001f * HoverBobFrequency) * HoverBobAmplitude;
            var meshPosition = DroneMesh.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, bobOffset, 0.1f);
            DroneMesh.Position = meshPosition;
        }
        else
        {
            // Return to local zero smoothly when actively flying
            var meshPosition = DroneMesh.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, 0.0f, 0.1f);
            DroneMesh.Position = meshPosition;
        }
    }
    
}
