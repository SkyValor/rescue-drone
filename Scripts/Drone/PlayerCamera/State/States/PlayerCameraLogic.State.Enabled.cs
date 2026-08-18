namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Enabled : State, IGet<Input.OnInputEvent>, IGet<Input.Disable>
        {
            public Enabled()
            {
                
            }

            public Transition On(in Input.OnInputEvent inputEvent)
            {
                var settings = Get<PlayerCameraSettings>();
                var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
                
                var @event = inputEvent.Event;

                if (@event.IsActionPressed("wheel_up")) OnWheelUp(playerCamera, settings.MinZoom);
                if (@event.IsActionPressed("wheel_down")) OnWheelDown(playerCamera, settings.MaxZoom);

                if (@event is not InputEventMouseMotion mouseMotion || !IsMouseCaptured()) return ToSelf();

                var cameraRotation = playerCamera.GetThirdPersonRotation();
                cameraRotation.Y -= mouseMotion.Relative.X * settings.MouseSensitivity;
                cameraRotation.X -= mouseMotion.Relative.Y * settings.MouseSensitivity;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));

                Output(new Output.RotationComputed(cameraRotation));
                return ToSelf();
            }

            private void OnWheelUp(PhantomCamera3D playerCamera, float minZoom)
            {
                var length = Mathf.Max(playerCamera.SpringLength - 1, minZoom);
                Output(new Output.ZoomComputed(length));
            }

            private void OnWheelDown(PhantomCamera3D playerCamera, float maxZoom)
            {
                var length = Mathf.Min(playerCamera.SpringLength + 1, maxZoom);
                Output(new Output.ZoomComputed(length));
            }
    
            private static bool IsMouseCaptured() => Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured;

            public Transition On(in Input.Disable input) => To<Disabled>();
        }
    }
}