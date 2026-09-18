namespace RescueDrone;

using Godot;

public partial class KeyboardMouseInputComponent : InputComponent
{
    [Export(PropertyHint.ResourceType, "KeyboardMouseControlScheme")] 
    public KeyboardMouseControlScheme ControlScheme { get; private set; }

    public override void PhysicsMovementUpdate()
    {
        HorizontalInput = GetHorizontalMoveInput();
        VerticalInput = GetVerticalInput();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var zoomIn = @event.IsActionPressed(GameInputs.CamZoomIn);
        var zoomOut = @event.IsActionPressed(GameInputs.CamZoomOut);

        if (zoomIn && !zoomOut) InvokeCameraZoomInput(CameraZoomType.ZoomIn);
        if (zoomOut && !zoomIn) InvokeCameraZoomInput(CameraZoomType.ZoomOut);

        if (@event is not InputEventMouseMotion mouseMotion) return;
        var camRotation = new Vector2
        {
            X = mouseMotion.Relative.Y * 0.001f,
            Y = mouseMotion.Relative.X * 0.001f
        };
        InvokeCameraRotationInput(camRotation);
    }
    
    private static Vector2 GetHorizontalMoveInput() => 
        Input.GetVector(GameInputs.MoveLeft, GameInputs.MoveRight, GameInputs.MoveForward, GameInputs.MoveBack);
    
    private static float GetVerticalInput() => Input.GetAxis(GameInputs.ThrottleDown, GameInputs.ThrottleUp);
    
}
