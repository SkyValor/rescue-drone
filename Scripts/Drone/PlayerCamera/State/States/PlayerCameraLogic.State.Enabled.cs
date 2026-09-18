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
            IGet<Input.OnCameraZoomInput>,
            IGet<Input.OnCameraRotationInput>, 
            IGet<Input.Disable>
        {
            public Enabled()
            {
                OnAttach(() =>
                {
                    var deviceHandler = Get<IGameRepo>().InputDeviceHandler.Value;
                    if (deviceHandler is null) return;
                    
                    var inputComponent = deviceHandler.CurrentInputComponent;
                    inputComponent.CameraZoomInput += OnCameraZoomInput;
                    inputComponent.CameraRotationInput += OnCameraRotationInput;
                });
                
                OnDetach(() =>
                {
                    var deviceHandler = Get<IGameRepo>().InputDeviceHandler.Value;
                    if (deviceHandler is null) return;
                    
                    var inputComponent = deviceHandler.CurrentInputComponent;
                    inputComponent.CameraZoomInput -= OnCameraZoomInput;
                    inputComponent.CameraRotationInput -= OnCameraRotationInput;
                });
            }
            
            public Transition On(in Input.Disable input) => To<Disabled>();

            private void OnCameraZoomInput(InputComponent.CameraZoomType cameraZoom) => Input(new Input.OnCameraZoomInput(cameraZoom));

            public Transition On(in Input.OnCameraZoomInput input)
            {
                var settings = Get<PlayerCameraSettings>();
                var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;

                var zoomType = input.ZoomType;
                if (zoomType is InputComponent.CameraZoomType.ZoomIn) 
                    OnZoomIn(playerCamera, settings.MinZoom);
                else 
                    OnZoomOut(playerCamera, settings.MaxZoom);
                
                return ToSelf();
            }
            
            private void OnZoomIn(PhantomCamera3D playerCamera, float minZoom)
            {
                var length = Mathf.Max(playerCamera.SpringLength - 1, minZoom);
                Output(new Output.ZoomComputed(length));
            }

            private void OnZoomOut(PhantomCamera3D playerCamera, float maxZoom)
            {
                var length = Mathf.Min(playerCamera.SpringLength + 1, maxZoom);
                Output(new Output.ZoomComputed(length));
            }

            private void OnCameraRotationInput(Vector2 cameraRelative) => Input(new Input.OnCameraRotationInput(cameraRelative));

            public Transition On(in Input.OnCameraRotationInput input)
            {
                var settings = Get<PlayerCameraSettings>();
                var playerCamera = Get<IGameRepo>().PlayerPhantomCamera.Value;
                
                var motionRelative = input.CameraRelative;
                var minAngle = settings.MinVerticalAngle;
                var maxAngle = settings.MaxVerticalAngle;
                
                var cameraRotation = playerCamera.GetThirdPersonRotation();
                cameraRotation.X -= motionRelative.X;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(minAngle), Mathf.DegToRad(maxAngle));
                cameraRotation.Y -= motionRelative.Y;
                cameraRotation.Y = Mathf.Wrap(cameraRotation.Y, 0f, Mathf.Tau);
                playerCamera.SetThirdPersonRotation(cameraRotation);
                return ToSelf();
            }
            
        }
    }
}
