namespace RescueDrone;

using Godot;

public partial class DroneMovement : Node
{
    [Export] public float Speed { get; set; } = 200f;

    private Drone drone;
    private Vector3 inputVector;

    public void SetInputVector(Vector3 inputVector)
    {
        this.inputVector = inputVector;
    }

    public void Tick(double delta)
    {
        drone.Velocity = inputVector * Speed;
        drone.MoveAndSlide();
    }
    
}
