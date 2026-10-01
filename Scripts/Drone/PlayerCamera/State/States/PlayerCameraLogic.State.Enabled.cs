namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;
using PhantomCamera;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Enabled : State,
            IGet<Input.OnPhysicsTick>, 
            IGet<Input.Disable>
        {
            public Enabled()
            {
                this.OnEnter(() =>
                {
                    var userSettings = Get<IGameRepo>().UserSettings.Value;
                    GD.Print(userSettings.MouseSensitivity.GetSensitivityMultiplier());
                    GD.Print(userSettings.AnalogSensitivity.GetSensitivityMultiplier());
                });
            }
            
            public Transition On(in Input.Disable input) => To<Disabled>();

            public Transition On(in Input.OnPhysicsTick input)
            {
                var gameRepo = Get<IGameRepo>();
                var settings = Get<PlayerCameraSettings>();
                var playerCamera = gameRepo.PlayerPhantomCamera.Value;
                var inputComponent = gameRepo.InputComponent.Value;
                
                // Check if there is camera zoom input
                var zoomInput = inputComponent.GetCameraZoomInput();
                if (zoomInput.IsNotZeroApprox())
                {
                    var springLength = playerCamera.SpringLength;
                    var minZoom = settings.MinZoom;
                    var maxZoom = settings.MaxZoom;

                    var length = Mathf.Clamp(springLength + zoomInput, minZoom, maxZoom);
                    if (!length.IsEqualApprox(minZoom) && !length.IsEqualApprox(maxZoom))
                        Output(new Output.ZoomComputed(length));
                }

                var rotationInput = inputComponent.GetCameraRotationInput();
                if (rotationInput.IsZeroApprox()) return ToSelf();
                
                var minAngle = settings.MinVerticalAngle;
                var maxAngle = settings.MaxVerticalAngle;
                var userSettings = gameRepo.UserSettings.Value;
                var cameraRotation = playerCamera.GetThirdPersonRotation();

                // TODO: Missing the Keyboard-only approach...
                
                if (IsMouseDeviceEnabled())
                {
                    // The rotation input is MouseMotion.Relative
                    var mouseSensitivity = userSettings.MouseSensitivity.GetSensitivityMultiplier();
                    var motionRelative = rotationInput * mouseSensitivity;
                    
                    cameraRotation.X -= motionRelative.Y;
                    cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
                    cameraRotation.Y -= motionRelative.X;
                    // cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau);
                }
                else if (IsJoypadDeviceEnabled())
                {
                    // The rotation input is from an analog stick
                    var analogSensitivity = userSettings.AnalogSensitivity.GetSensitivityMultiplier();
                    var analogRelative = rotationInput * analogSensitivity;
                    
                    cameraRotation.X -= analogRelative.Y;
                    cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
                    cameraRotation.Y -= analogRelative.X;
                }
                
                playerCamera.SetThirdPersonRotation(cameraRotation);
                return ToSelf();
            }

            private bool IsMouseDeviceEnabled()
            {
                var gameRepo = Get<IGameRepo>();
                var deviceHandler = gameRepo.DeviceHandler.Value;
                
                return
                    deviceHandler.CurrentDeviceType.Value is InputDeviceType.Computer &&
                    deviceHandler.CurrentDeviceScheme.Value is InputDeviceScheme.KeyboardAndMouse;
            }

            private bool IsJoypadDeviceEnabled()
            {
                var gameRepo = Get<IGameRepo>();
                var deviceHandler = gameRepo.DeviceHandler.Value;
                return deviceHandler.CurrentDeviceType.Value is InputDeviceType.Joypad;
            }

            // public Transition On(in Input.OnCameraRotationInput input)
            // {
            //     var settings = Get<PlayerCameraSettings>();
            //     var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
            //     
            //     var motionRelative = input.CameraRelative;
            //     var minAngle = settings.MinVerticalAngle;
            //     var maxAngle = settings.MaxVerticalAngle;
            //     
            //     var cameraRotation = playerCamera.GetThirdPersonRotation();
            //     cameraRotation.X -= motionRelative.X;
            //     cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
            //     cameraRotation.Y -= motionRelative.Y;
            //     cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau);
            //     playerCamera.SetThirdPersonRotation(cameraRotation);
            //     return ToSelf();
            // }
            
        }
    }
}
