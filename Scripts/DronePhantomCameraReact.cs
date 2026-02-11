namespace RescueDrone;

using Godot;
using PhantomCamera;

public partial class DronePhantomCameraReact : Node
{
    [Export] private Drone Drone { get; set; }
    [ExportGroup("Yaw React")]
    [Export] private float SideOffsetMax { get; set; } = 1.5f;
    [Export] private float SideLookAwayOffset { get; set; } = 0.85f;
    [Export] private float SideLookAwayDelta { get; set; } = 0.125f;
    [Export] private float SideLookAwayReturnDelta { get; set; } = 0.35f;
    [ExportGroup("Throttle React")]
    [Export] private float ThrottleOffsetMax { get; set; } = 0.85f;

    private PhantomCamera3D pCam;
    private Vector3 defaultPosition;
    private Vector3 defaultLookAtOffset;
    private float currentYawForce;
    
    public override void _Ready()
    {
        base._Ready();
        pCam = GetParent<Node3D>().AsPhantomCamera3D();
        defaultPosition = pCam.Node3D.Position;
        defaultLookAtOffset = pCam.LookAtOffset;

        if (Drone?.Controller is null)
            return;

        Drone.Controller.YawInput += OnYawInput;
        Drone.Controller.ThrottleInput += OnThrottleInput;
    }

    private void OnYawInput(float input)
    {
        currentYawForce = Mathf.MoveToward(currentYawForce, input, currentYawForce < input 
            ? SideLookAwayDelta 
            : SideLookAwayReturnDelta);

        // Look away from the center in the direction the player is turning to
        pCam.LookAtOffset = pCam.LookAtOffset with { X = currentYawForce * SideLookAwayOffset };
        pCam.Node3D.Position = pCam.Node3D.Position with
        {
            // Offset away from the center, in the direction the player is turning to
            X = defaultPosition.X + currentYawForce * SideOffsetMax,
        };
    }

    private void OnThrottleInput(float input)
    {
        pCam.LookAtOffset = pCam.LookAtOffset with { Y = defaultLookAtOffset.Y + input * ThrottleOffsetMax };
    }
    
}
