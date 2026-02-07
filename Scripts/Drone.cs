namespace RescueDrone;

using Godot;

// TODO: We might have a Curve for the acceleration (forward) and another Curve for the deceleration (zero but still forward).
// Same thing for backward movement and strafing.

public partial class Drone : CharacterBody3D
{
    [Export] public DroneController Controller { get; set; }
    [Export] private Camera3D Camera { get; set; }
    [ExportGroup("Movement")]
    // [Export] public float Speed { get; set; } = 200f;
    [Export] private Curve AccelCurve { get; set; }
    [Export] private float MaxPropulsionSpeed { get; set; } = 200f;
    [Export] private float AccelWeight { get; set; } = 0.5f;
    [Export] private float AccelLossWeight { get; set; } = 0.75f;
    [Export] private float TimeToMaxSpeed { get; set; } = 5f;
    [ExportGroup("Strafing")]
    [Export] private Curve StrafingCurve { get; set; }
    [Export] private float MaxStrafingSpeed { get; set; } = 80f;
    [Export] private float StrafingWeight { get; set; } = 0.5f;
    [Export] private float TimeToMaxStrafingSpeed { get; set; } = 3f;
    [ExportGroup("Rotation")]
    [Export] public float RotationSpeed { get; set; } = 50f;

    private float currentVelocity;
    private float currentAcceleration;

    private float curveT;
    
    private Vector3 inputVector = Vector3.Zero;
    private float inputRotation;
    private bool isAccelerating = true;
    
    public override void _Ready()
    {
        base._Ready();
        Controller.SetDrone(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        Controller.Tick(delta);

        RotateDrone(delta);
        MoveDrone((float) delta);
    }

    private void RotateDrone(double delta)
    {
        if (inputRotation == 0) 
            return;
        
        var rotation = Rotation;
        var targetRotation = inputRotation < 0f ? rotation.Y - 50f : rotation.Y + 50f;
        rotation.Y = (float) Mathf.LerpAngle(rotation.Y, targetRotation, RotationSpeed * delta);
        Rotation = rotation;
    }
        
    private void MoveDrone(float delta)
    {
        // currentAcceleration = inputVector != Vector3.Zero
            // ? currentAcceleration + inputVector.Z * AccelWeight * delta
            // : Mathf.MoveToward(currentAcceleration, 0f, AccelLossWeight * delta);
        
        // var speed = AccelCurve.Sample(currentAcceleration);

        if (inputVector.Z < 0f)
        {
            curveT = Mathf.Min(1f, curveT + 0.02f);
            GD.Print("Curve T: " + curveT);
        }
        else if (inputVector.Z == 0f)
        {
            // TODO: Check whether current velocity is positive (forward) or negative (backward) to apply subtraction or addition
            curveT = Mathf.Max(0f, curveT - 0.02f);
            GD.Print("Curve T: " + curveT);
        }

        var speed = AccelCurve.Sample(curveT) * MaxPropulsionSpeed;

        // Set direction relative to camera
        var camRotation = Camera.GlobalTransform.Basis.GetEuler().Y;
        var moveDirection = inputVector
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        
        Velocity = moveDirection * speed * delta;
        MoveAndSlide();
    }

    public void SetInputVector(Vector3 inputVector)
    {
        this.inputVector = inputVector;
    }
    
    public void SetInputRotation(float rotation)
    {
        inputRotation = rotation;
    }
    
}
