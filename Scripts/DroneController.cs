namespace RescueDrone;

using Godot;

public partial class DroneController : Node
{
    protected Drone Drone;

    public void SetDrone(Drone drone)
    {
        Drone = drone;
    }

    public virtual void Tick(double delta)
    {
        
    }
    
}
