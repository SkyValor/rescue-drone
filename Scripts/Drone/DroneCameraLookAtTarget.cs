namespace RescueDrone;

using Godot;

/// <summary>
/// This object will react to a <see cref="Drone"/>'s movement input events being invoked
/// and will move accordingly. A main offset is set to ensure this target is always at that position
/// relative to the drone. Other offsets are also available to be added as extra distance in case of
/// specific drone movement.
/// </summary>
public partial class DroneCameraLookAtTarget : Node3D
{
    [Export] private Drone Drone { get; set; }
    [Export] private Vector3 MainOffset { get; set; }
    [Export] private Vector3 OnYawOffsetAdditive { get; set; }
    [Export] private Vector3 OnThrottleOffsetAdditive { get; set; }

    private Vector3 additiveOffset;
    private float yawInput;
    private float throttleInput;

    public override void _Ready()
    {
        Drone.Controller.YawInput += OnYawInput;
        Drone.Controller.ThrottleInput += OnThrottleInput;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Drone is null)
            return;
        
        SetPositionRelativeToDrone();
    }

    public void SetDrone(Drone drone) => Drone = drone;
    
    private void OnYawInput(float input) => yawInput = input;
    private void OnThrottleInput(float input) => throttleInput = input;

    private void SetPositionRelativeToDrone()
    {
        var droneRotation = Drone.GlobalTransform.Basis.GetEuler();
        var mainOffsetRelative = MainOffset.Rotated(Vector3.Up, droneRotation.Y);
        
        var onYawOffset = OnYawOffsetAdditive * yawInput;
        var onYawOffsetRelative = onYawOffset.Rotated(Vector3.Up, droneRotation.Y);
        
        var onThrottleOffset = OnThrottleOffsetAdditive * throttleInput;
        var onThrottleOffsetRelative = onThrottleOffset.Rotated(Vector3.Up, droneRotation.Y);
        
        GlobalPosition = Drone.GlobalPosition + mainOffsetRelative + onYawOffsetRelative + onThrottleOffsetRelative;
    }
    
}
