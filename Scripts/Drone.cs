namespace RescueDrone;

using Godot;

// TODO: We might have a Curve for the acceleration (forward) and another Curve for the deceleration (zero but still forward).
// Same thing for backward movement and strafing.

public partial class Drone : CharacterBody3D
{
    [Export] public DroneController Controller { get; set; }
    [Export] private Camera3D Camera { get; set; }
    [Export] private DroneMovement Movement { get; set; }
    
    private float currentVelocity;
    private float currentAcceleration;

    private float curveT;
    
    private Vector3 inputVector = Vector3.Zero;
    private float inputRotation;
    private bool isAccelerating = true;
    
    public override void _Ready()
    {
        base._Ready();
        Movement?.SetDrone(this);
        
        if (Controller is null)
            return;
        
        Controller.PropulsionInput += OnPropulsionInput;
        Controller.StrafingInput += OnStrafingInput;
        Controller.TurnInput += OnTurnInput;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        var deltaFloat = (float) delta;
        Controller?.Tick(deltaFloat);
        Movement?.Tick(deltaFloat);
    }

    private void OnPropulsionInput(float input) => Movement?.SetPropulsionIntent(input);

    private void OnStrafingInput(float input) => Movement?.SetStrafingIntent(input);
    
    private void OnTurnInput(float input) => Movement?.SetTurningIntent(input);

}
