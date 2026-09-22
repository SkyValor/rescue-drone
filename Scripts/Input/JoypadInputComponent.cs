namespace RescueDrone;

using Godot;

public partial class JoypadInputComponent : InputComponent
{
    
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventJoypadMotion motion)
        {
            var axis = motion.Axis;
            var axisValue = motion.AxisValue;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // For joypads, to zoom in/out, we need to hold the trigger as a composite
        var trigger = @event.IsAction(GameInputs.JoypadCamZoomComposite);
        if (trigger)
        {
            var zoomIn = @event.IsActionPressed(GameInputs.JoypadCamZoomIn);
            var zoomOut = @event.IsActionPressed(GameInputs.JoypadCamZoomOut);
            
            if (zoomIn && !zoomOut) InvokeCameraZoomInput(CameraZoomType.ZoomIn);
            if (zoomOut && !zoomIn) InvokeCameraZoomInput(CameraZoomType.ZoomOut);
        }
        else
        {
            
        }
    }

    public override void PhysicsMovementUpdate()
    {
        
    }
}
