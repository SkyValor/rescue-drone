namespace RescueDrone.Scripts;

using Godot;
using PhantomCamera;

public partial class PlayerMover : CharacterBody3D
{
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
    [Export] public bool RotateWithMouse { get; private set; }
    [Export] public float CamRotationSpeed = 3.0f;

    private PhantomCamera3D pCam;
    private float target_rotation_y;
    
    public override void _Ready()
    {
        // Floating mode ensures move_and_slide handles 3D space movement without floor snaps
        MotionMode = MotionModeEnum.Floating;
        
        if (DroneMesh == null && HasNode("DroneMesh"))
            DroneMesh = GetNode<Node3D>("DroneMesh");

        pCam = GetNode<Node3D>("../PlayerThirdPersonCamera").AsPhantomCamera3D();
        if (RotateWithMouse && pCam.FollowMode == FollowMode3D.ThirdPerson)
            Input.SetMouseMode(Input.MouseModeEnum.Captured);
        
        // _UnhandledInput is only called if we need to read mouse motion
        SetProcessUnhandledInput(RotateWithMouse);
    }

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

    // public override void _PhysicsProcess(double delta)
    // {
    //     var deltaTime = (float) delta;
    //     
    //     if (!RotateWithMouse)
    //         RotateCameraWithKeys(deltaTime);
    //     
    //     // 1. Handle Yaw Rotation (Turning left/right)
    //     var rotationInput = Input.GetAxis("turn_right", "turn_left");
    //     RotateY(rotationInput * RotationSpeed * deltaTime);
    //
    //     // 2. Gather Translation Input Vectors
    //     var inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    //     var verticalInput = Input.GetAxis("throttle_down", "throttle_up");
    //
    //     // Calculate horizontal direction based on the drone's current rotation basis
    //     var direction = (Transform.Basis.X * inputDirection.X) + (Transform.Basis.Z * inputDirection.Y);
    //     if (direction.LengthSquared() > 1.0f)
    //         direction = direction.Normalized();
    //
    //     // 3. Apply Separate Acceleration and Friction logic
    //     var currentVelocity = Velocity;
    //
    //     // --- Horizontal Velocity Logic ---
    //     var targetHorizontalVelocity = direction * MaxHorizontalSpeed;
    //     var currentHorizontalVelocity = new Vector3(currentVelocity.X, 0, currentVelocity.Z);
    //
    //     // Use Acceleration if pushing input, Deceleration (friction) if drifting
    //     var horizontalStep = (direction.LengthSquared() > 0) ? Acceleration : Deceleration;
    //     currentHorizontalVelocity = currentHorizontalVelocity.MoveToward(targetHorizontalVelocity, horizontalStep * deltaTime);
    //
    //     // --- Vertical Velocity Logic ---
    //     var targetVerticalVelocity = verticalInput * MaxVerticalSpeed;
    //     var currentVerticalStep = (Mathf.Abs(verticalInput) > 0) ? Acceleration : Deceleration;
    //     var verticalVelocity = Mathf.MoveToward(currentVelocity.Y, targetVerticalVelocity, currentVerticalStep * deltaTime);
    //
    //     Velocity = new Vector3(currentHorizontalVelocity.X, verticalVelocity, currentHorizontalVelocity.Z);
    //     MoveAndSlide();
    //     
    //     // 4. Procedural Drone Polish (Visuals)
    //     if (DroneMesh == null) return;
    //     ApplyVisualTilt(currentHorizontalVelocity, deltaTime);
    //     ApplyHoverBob(inputDirection, verticalInput);
    // }

    public override void _PhysicsProcess(double delta)
    {
        var deltaTime = (float) delta;
        
        // 1. Gather Movement Inputs
        var inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var verticalInput = Input.GetAxis("throttle_down", "throttle_up");

        // 2. Calculate Direction Relative to Camera
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

            // Combine camera perspective with player keyboard/joystick inputs
            direction = (camRight * inputDirection.X) + (camForward * inputDirection.Y);
        }
        else
        {
            // Fallback to global space if no camera is present
            direction = new Vector3(inputDirection.X, 0, inputDirection.Y);
        }

        if (direction.LengthSquared() > 1.0f)
            direction = direction.Normalized();

        // 3. Optional: Automatically rotate the drone's nose to face the direction it is traveling
        if (direction.LengthSquared() > 0.01f)
        {
            // Calculates the angle on the Y axis towards the movement vector
            var targetTargetAngle = Mathf.Atan2(-direction.X, -direction.Z);
            
            // Smoothly rotate the actual drone body to face where it's going
            var droneRot = Rotation;
            droneRot.Y = Mathf.LerpAngle(droneRot.Y, targetTargetAngle, RotationSpeed * deltaTime);
            Rotation = droneRot;
        }

        // 4. Smooth Velocity Processing
        var currentVelocity = Velocity;
        var targetHorizontalVelocity = direction * MaxHorizontalSpeed;
        var currentHorizontalVelocity = new Vector3(currentVelocity.X, 0, currentVelocity.Z);

        var horizontalStep = (direction.LengthSquared() > 0) ? Acceleration : Deceleration;
        currentHorizontalVelocity = currentHorizontalVelocity.MoveToward(targetHorizontalVelocity, horizontalStep * deltaTime);

        var targetVerticalVelocity = verticalInput * MaxVerticalSpeed;
        var currentVerticalStep = (Mathf.Abs(verticalInput) > 0) ? Acceleration : Deceleration;
        var verticalVelocity = Mathf.MoveToward(currentVelocity.Y, targetVerticalVelocity, currentVerticalStep * deltaTime);

        Velocity = new Vector3(currentHorizontalVelocity.X, verticalVelocity, currentHorizontalVelocity.Z);
        MoveAndSlide();

        // 5. Procedural Drone Polish (Visuals)
        if (DroneMesh != null)
        {
            ApplyVisualTilt(currentHorizontalVelocity, deltaTime);
            ApplyHoverBob(inputDirection, verticalInput);
        }
    }

    private void RotateCameraWithKeys(float deltaTime)
    {
        var currentRotation = pCam.GetThirdPersonRotation();
            
        // Get button inputs for turning
        if (Input.IsActionPressed("cam_rotate_left"))
            target_rotation_y += CamRotationSpeed * deltaTime;
        if (Input.IsActionPressed("cam_rotate_right"))
            target_rotation_y -= CamRotationSpeed * deltaTime;
		
        // Smoothly interpolate the rotation so it's not jerky
        currentRotation.Y = Mathf.LerpAngle(currentRotation.Y, target_rotation_y, CamRotationSpeed * deltaTime);
        pCam.SetThirdPersonRotation(currentRotation);
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
