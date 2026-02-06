namespace RescueDrone;

using Godot;

public partial class Drone : CharacterBody3D
{
    [Export] public DroneController Controller { get; set; }
    [Export] public DroneMovement Movement { get; set; }
    [Export] public float Speed { get; set; } = 200f;
    [Export] public float RotationSpeed { get; set; } = 50f;
    [Export] private Camera3D Camera { get; set; }
    [Export] private Curve AccelCurve { get; set; }
    
    private Vector3 inputVector = Vector3.Zero;
    private float inputRotation;
    
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
        MoveDrone(delta);
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
        
    private void MoveDrone(double delta)
    {
        // Set direction relative to camera
        var camRotation = Camera.GlobalTransform.Basis.GetEuler().Y;
        var moveDirection = inputVector
            .Rotated(Vector3.Up, camRotation)
            .Normalized();
        
        Velocity = moveDirection * Speed * (float) delta;
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
