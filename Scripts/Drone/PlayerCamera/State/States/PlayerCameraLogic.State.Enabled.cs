namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Enabled : State, IGet<Input.Disable>, IGet<Input.OnPhysicsTick>
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

            private void HandleCameraZoomInput(IGameRepo gameRepo, PlayerCameraSettings cameraSettings)
            {
                var inputComponent = Get<InputComponent>();
                var zoomInput = inputComponent.GetCameraZoomInput();
                
                if (zoomInput.IsZeroApprox()) return;
                
                var playerCamera = gameRepo.PlayerPhantomCamera.Value;
                var springLength = playerCamera.SpringLength;
                var minZoom = cameraSettings.MinZoom;
                var maxZoom = cameraSettings.MaxZoom;
                
                var targetLength = Mathf.Clamp(springLength + zoomInput, minZoom, maxZoom);
                Output(new Output.ZoomComputed(targetLength));
            }

            private void HandleCameraRotationInput(IGameRepo gameRepo, PlayerCameraSettings cameraSettings)
            {
                var inputComponent = Get<InputComponent>();
                var rotationInput = inputComponent.GetCameraRotationInput();
                
                if (rotationInput.IsZeroApprox()) return;

                var userSettings = gameRepo.UserSettings.Value;
                HandleCameraInversion(ref rotationInput, userSettings);

                var inputType = gameRepo.DeviceHandler.Value.CurrentInputType.Value;
                var sensitivitySettings = userSettings.GetSensitivitySettings(inputType);
                var inputDelta = rotationInput * sensitivitySettings.GetSensitivityMultiplier();
                
                var playerCamera = gameRepo.PlayerPhantomCamera.Value;
                var cameraRotation = playerCamera.GetThirdPersonRotation();
                ComputeCameraRotation(ref cameraRotation, cameraSettings, inputDelta);
                Output(new Output.RotationComputed(cameraRotation));
            }

            private static void HandleCameraInversion(ref Vector2 rotationInput, UserSettings settings)
            {
                if (settings.InvertCameraXAxis) rotationInput.X = -rotationInput.X;
                if (settings.InvertCameraYAxis) rotationInput.Y = -rotationInput.Y;
            }
            
            private static void ComputeCameraRotation(ref Vector3 cameraRotation, PlayerCameraSettings cameraSettings, Vector2 inputDelta)
            {
                var minAngle = cameraSettings.MinVerticalAngle;
                var maxAngle = cameraSettings.MaxVerticalAngle;
                    
                cameraRotation.X -= inputDelta.Y;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
                cameraRotation.Y -= inputDelta.X;
                // cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau);
            }
            
        }
    }
}
