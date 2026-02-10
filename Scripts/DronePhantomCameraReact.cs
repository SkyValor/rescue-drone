namespace RescueDrone;

using Godot;
using PhantomCamera;

public partial class DronePhantomCameraReact : Node
{
    [Export] private Drone Drone { get; set; }

    private PhantomCamera3D pCam;

    public override void _Ready()
    {
        base._Ready();
        pCam = GetParent<Node3D>().AsPhantomCamera3D();

        if (Drone?.Controller is null)
            return;

        Drone.Controller.PitchInput += OnPitchInput;
        Drone.Controller.RollInput += OnRollInput;
        Drone.Controller.YawInput += OnYawInput;
        Drone.Controller.ThrottleInput += OnThrottleInput;
    }

    private void OnPitchInput(float obj)
    {
        
    }
    
    private void OnRollInput(float obj)
    {
        
    }

    private void OnYawInput(float obj)
    {
        
    }

    private void OnThrottleInput(float obj)
    {
        
    }
    
}
