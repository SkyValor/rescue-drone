namespace RescueDrone;

using Godot;
using PhantomCamera;

/// <summary>
/// This composition node has the single responsibility of ensuring its parent <see cref="PhantomCamera3D"/>
/// follows the designated <see cref="Drone"/> whenever events for movement input are invoked.
/// <para/>
/// No offset needs to be set in this class. Instead, it gets the initial <see cref="Vector3"/> offset from
/// the drone and will make sure to update the camera's relative position using it.
/// </summary>
public partial class DronePhantomCameraFollow : Node
{
    [Export] private Drone Drone { get; set; }

    private PhantomCamera3D pCam;
    private Vector3 offsetFromDrone;
    
    public override void _Ready()
    {
        base._Ready();
        pCam = GetParent<Node3D>().AsPhantomCamera3D();
        offsetFromDrone = new Vector3(
            x: Mathf.Abs(Drone.GlobalPosition.X - pCam.Node3D.GlobalPosition.X),
            y: Mathf.Abs(Drone.GlobalPosition.Y - pCam.Node3D.GlobalPosition.Y),
            z: Mathf.Abs(Drone.GlobalPosition.Z - pCam.Node3D.GlobalPosition.Z));

        if (Drone?.Controller is null)
            return;

        Drone.Controller.PitchInput += SetCameraPositionWithOffset;
        Drone.Controller.RollInput += SetCameraPositionWithOffset;
        Drone.Controller.YawInput += SetCameraPositionWithOffset;
        Drone.Controller.ThrottleInput += SetCameraPositionWithOffset;
    }

    private void SetCameraPositionWithOffset(float _)
    {
        var droneRotation = Drone.GlobalTransform.Basis.GetEuler();
        var offsetRelative = offsetFromDrone.Rotated(Vector3.Up, droneRotation.Y);
        pCam.Node3D.GlobalPosition = Drone.GlobalPosition + offsetRelative;
    }
    
}
