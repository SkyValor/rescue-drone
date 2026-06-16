namespace RescueDrone.Scripts;

using Godot;

public partial class PlayerMover : CharacterBody3D
{
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
    
    public override void _Ready()
    {
        // Floating mode ensures move_and_slide handles 3D space movement without floor snaps
        MotionMode = MotionModeEnum.Floating;
        
        if (DroneMesh == null && HasNode("DroneMesh"))
            DroneMesh = GetNode<Node3D>("DroneMesh");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        var deltaTime = (float) delta;
        
        // 1. Handle Yaw Rotation (Turning left/right)
        var rotationInput = Input.GetAxis("turn_right", "turn_left");
        RotateY(rotationInput * RotationSpeed * deltaTime);

        // 2. Gather Translation Input Vectors
        var inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var verticalInput = Input.GetAxis("throttle_down", "throttle_up");

        // Calculate horizontal direction based on the drone's current rotation basis
        var direction = (Transform.Basis.X * inputDirection.X) + (Transform.Basis.Z * inputDirection.Y);
        if (direction.LengthSquared() > 1.0f)
            direction = direction.Normalized();

        // 3. Apply Separate Acceleration and Friction logic
        var currentVelocity = Velocity;

        // --- Horizontal Velocity Logic ---
        var targetHorizontalVelocity = direction * MaxHorizontalSpeed;
        var currentHorizontalVelocity = new Vector3(currentVelocity.X, 0, currentVelocity.Z);

        // Use Acceleration if pushing input, Deceleration (friction) if drifting
        var horizontalStep = (direction.LengthSquared() > 0) ? Acceleration : Deceleration;
        currentHorizontalVelocity = currentHorizontalVelocity.MoveToward(targetHorizontalVelocity, horizontalStep * deltaTime);

        // --- Vertical Velocity Logic ---
        var targetVerticalVelocity = verticalInput * MaxVerticalSpeed;
        var currentVerticalStep = (Mathf.Abs(verticalInput) > 0) ? Acceleration : Deceleration;
        var verticalVelocity = Mathf.MoveToward(currentVelocity.Y, targetVerticalVelocity, currentVerticalStep * deltaTime);

        Velocity = new Vector3(currentHorizontalVelocity.X, verticalVelocity, currentHorizontalVelocity.Z);
        MoveAndSlide();
        
        // 4. Procedural Drone Polish (Visuals)
        if (DroneMesh == null) return;
        ApplyVisualTilt(currentHorizontalVelocity, deltaTime);
        ApplyHoverBob(inputDirection, verticalInput);
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
