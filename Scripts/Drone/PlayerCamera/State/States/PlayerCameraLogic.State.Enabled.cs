namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Enabled : State, 
            IGet<Input.OnInputEvent>, 
            IGet<Input.OnProcessTick>, 
            IGet<Input.Disable>
        {
            private Vector3 cameraRotationTarget = Vector3.Zero;
            
            public Enabled()
            {
                OnAttach(() =>
                {
                    var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
                    cameraRotationTarget = playerCamera.GetThirdPersonRotation();
                    GD.Print(cameraRotationTarget);
                });
            }
            
            public Transition On(in Input.Disable input) => To<Disabled>();
            
            public Transition On(in Input.OnProcessTick input)
            {
                var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
                var currentRotation = playerCamera.GetThirdPersonRotation();

                if (currentRotation.IsEqualApprox(cameraRotationTarget)) return ToSelf();

                var lerpPower = Get<PlayerCameraSettings>().LerpPower;
                var smoothRotation = currentRotation.Lerp(cameraRotationTarget, (float) input.Delta * lerpPower);
                playerCamera.SetThirdPersonRotation(smoothRotation);
                return ToSelf();
            }

            public Transition On(in Input.OnInputEvent inputEvent)
            {
                var settings = Get<PlayerCameraSettings>();
                var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
                
                var @event = inputEvent.Event;

                if (@event is InputEventKey { Pressed: true, Keycode: Key.K }) GD.Print(cameraRotationTarget);
                
                if (@event.IsActionPressed("wheel_up")) OnWheelUp(playerCamera, settings.MinZoom);
                if (@event.IsActionPressed("wheel_down")) OnWheelDown(playerCamera, settings.MaxZoom);

                if (@event is not InputEventMouseMotion mouseMotion || !IsMouseCaptured()) return ToSelf();

                var cameraRotation = cameraRotationTarget;
                cameraRotation.X -= mouseMotion.Relative.Y * settings.MouseSensitivity;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(settings.MinVerticalAngle), Mathf.DegToRad(settings.MaxVerticalAngle));
                
                cameraRotation.Y -= mouseMotion.Relative.X * settings.MouseSensitivity;
                // cameraRotation.Y = Mathf.Clamp(cameraRotation.Y, 0f, Mathf.Tau);
                cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau); // Between 0 and 360 degrees
                // playerCamera.SetThirdPersonRotation(cameraRotation);
                cameraRotationTarget = cameraRotation;
                return ToSelf();

                // Output(new Output.RotationComputed(cameraRotation));
                // return ToSelf();
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
            
        }
    }
}
