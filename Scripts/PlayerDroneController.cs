namespace RescueDrone;

using Godot;

public partial class PlayerDroneController : DroneController
{
    public override void Tick(double delta)
    {
        base.Tick(delta);
        var propulsion = Input.GetAxis("move_back", "move_forward");
        if (propulsion != 0) OnPropulsionInput(propulsion);

        var strafing = Input.GetAxis("move_left", "move_right");
        if (strafing != 0) OnStrafingInput(strafing);

        var turning = Input.GetAxis("turn_left", "turn_right");
        if (turning != 0f) OnTurnInput(turning);
    }

}
