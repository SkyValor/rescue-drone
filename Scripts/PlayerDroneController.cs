namespace RescueDrone.Player;

using Godot;
using Vector3 = Godot.Vector3;

public partial class PlayerDroneController : DroneController
{
    public override void Tick(double delta)
    {
        base.Tick(delta);
        var inputVector = new Vector3(
            x: Input.GetAxis("move_left", "move_right"),
            y: 0f,
            z: Input.GetAxis("move_forward", "move_back"));
        Drone.SetInputVector(inputVector);

        var turningVector = Input.GetAxis("turn_left", "turn_right");
        Drone.SetInputRotation(turningVector);
    }

}
