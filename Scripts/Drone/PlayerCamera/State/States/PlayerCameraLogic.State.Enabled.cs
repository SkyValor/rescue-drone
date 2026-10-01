namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Enabled : State, IGet<Input.OnPhysicsTick>, IGet<Input.Disable>
        {
            public Transition On(in Input.Disable input) => To<Disabled>();

            public Transition On(in Input.OnPhysicsTick input)
            {
                var gameRepo = Get<IGameRepo>();
                var settings = Get<PlayerCameraSettings>();
                
                HandleCameraZoomInput(gameRepo, settings);
                HandleCameraRotationInput(gameRepo, settings);
                
                return ToSelf();
            }

            private void HandleCameraZoomInput(IGameRepo gameRepo, PlayerCameraSettings settings)
            {
                var inputComponent = gameRepo.InputComponent.Value;
                var zoomInput = inputComponent.GetCameraZoomInput();

                if (zoomInput.IsZeroApprox()) return;
                
                var playerCamera = gameRepo.PlayerPhantomCamera.Value;
                var springLength = playerCamera.SpringLength;
                var minZoom = settings.MinZoom;
                var maxZoom = settings.MaxZoom;
                
                var targetLength = Mathf.Clamp(springLength + zoomInput, minZoom, maxZoom);
                    Output(new Output.ZoomComputed(targetLength));
            }

            private static void HandleCameraRotationInput(IGameRepo gameRepo, PlayerCameraSettings settings)
            {
                var inputComponent = gameRepo.InputComponent.Value;
                var rotationInput = inputComponent.GetCameraRotationInput();
                
                if (rotationInput.IsZeroApprox()) return;
                
                var deviceHandler = gameRepo.DeviceHandler.Value;
                var userSettings = gameRepo.UserSettings.Value;
                var playerCamera = gameRepo.PlayerPhantomCamera.Value;
                var cameraRotation = playerCamera.GetThirdPersonRotation();

                // TODO: We need to handle proper XY-Inversion
                
                if (IsPlayerUsingMouse(deviceHandler))
                {
                    // The rotation input is MouseMotion.Relative
                    var mouseSensitivity = userSettings.MouseSensitivity.GetSensitivityMultiplier();
                    var motionRelative = rotationInput * mouseSensitivity;
                    ComputeCameraRotation(ref cameraRotation, settings, motionRelative);
                }
                else if (IsPlayerUsingKeyboard(deviceHandler))
                {
                    // The rotation input is from key pressed, therefore always magnitude 1.0
                    var keyboardSensitivity = userSettings.KeyboardSensitivity.GetSensitivityMultiplier();
                    var keyboardRelative = rotationInput * keyboardSensitivity;
                    ComputeCameraRotation(ref cameraRotation, settings, keyboardRelative);
                }
                else if (IsPlayerUsingJoypad(deviceHandler))
                {
                    // The rotation input is from an analog stick
                    var analogSensitivity = userSettings.AnalogSensitivity.GetSensitivityMultiplier();
                    var analogRelative = rotationInput * analogSensitivity;
                    ComputeCameraRotation(ref cameraRotation, settings, analogRelative);
                }
                
                playerCamera.SetThirdPersonRotation(cameraRotation);
            }

            private static bool IsPlayerUsingMouse(InputDeviceHandler deviceHandler) =>
                deviceHandler.CurrentDeviceScheme.Value is InputDeviceScheme.KeyboardAndMouse;

            private static bool IsPlayerUsingKeyboard(InputDeviceHandler deviceHandler) =>
                deviceHandler.CurrentDeviceScheme.Value is InputDeviceScheme.Keyboard;

            private static bool IsPlayerUsingJoypad(InputDeviceHandler deviceHandler) =>
                deviceHandler.CurrentInputType.Value is InputType.Joypad;
            
            private static void ComputeCameraRotation(ref Vector3 cameraRotation, PlayerCameraSettings settings, Vector2 inputDelta)
            {
                var minAngle = settings.MinVerticalAngle;
                var maxAngle = settings.MaxVerticalAngle;
                    
                cameraRotation.X -= inputDelta.Y;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
                cameraRotation.Y -= inputDelta.X;
                // cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau);
            }
            
        }
    }
}
