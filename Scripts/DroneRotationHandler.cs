namespace RescueDrone;

using Godot;

public partial class DroneRotationHandler : Node
{
    private Drone drone;
    private Vector3 inputVector;

    public void SetDrone(Drone drone) => this.drone = drone;
    
    public void SetInputVector(Vector3 vector) => inputVector = vector;

    public void Tick(float delta)
    {
        
    }
    
}
