namespace RescueDrone;

using Godot;

public partial class PlayerDroneController : DroneController
{
    public override void Tick()
    {
        base.Tick();
        OnPropulsionInput(Input.GetAxis("move_back", "move_forward"));
        OnStrafingInput(Input.GetAxis("move_left", "move_right"));
        OnTurnInput(Input.GetAxis("turn_left", "turn_right"));
        OnThrottleInput(Input.GetAxis("throttle_down", "throttle_up"));
    }

}
