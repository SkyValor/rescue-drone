namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class CameraSpringArm : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    #region Exports
    [Export] public float MouseSensitivity { get; private set; } = 0.005f;

    [Export(PropertyHint.Range, "-90.0, 0.0, 0.1")]
    public float MinVerticalAngle { get; private set; } = -90f;

    [Export(PropertyHint.Range, "0.0, 90.0, 0.1")]
    public float MaxVerticalAngle { get; private set; } = 45f;

    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MinZoom { get; private set; } = 2f;
    
    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MaxZoom { get; private set; } = 12f;
    #endregion
    
    [Node] private SpringArm3D SpringArm { get; set; }

    public void OnReady()
    {
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && IsMouseCaptured())
        {
            var rotation = Rotation;
            rotation.Y -= mouseMotion.Relative.X * MouseSensitivity;
            rotation.Y = Mathf.Wrap(rotation.Y, 0f, Mathf.Tau); // Between 0 and 360 degrees
        
            rotation.X -= mouseMotion.Relative.Y * MouseSensitivity;
            rotation.X = Mathf.Clamp(rotation.X, Mathf.DegToRad(MinVerticalAngle), Mathf.DegToRad(MaxVerticalAngle));
            Rotation = rotation;
        }

        if (@event.IsActionPressed("wheel_up")) SpringArm.SpringLength = Mathf.Max(SpringArm.SpringLength - 1, MinZoom);
        if (@event.IsActionPressed("wheel_down")) SpringArm.SpringLength = Mathf.Min(SpringArm.SpringLength + 1, MaxZoom);

        if (@event.IsActionPressed("toggle_mouse_capture"))
            Input.SetMouseMode(IsMouseCaptured() ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured);
    }
    
    private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
    
}
