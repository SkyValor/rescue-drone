namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

[Meta(typeof(IAutoNode))]
public partial class PlayerCamera : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    #region Exports
    [Export] public float MouseSensitivity { get; set; } = 0.005f;
    
    [Export(PropertyHint.Range, "-90.0, 0.0, 0.1")]
    public float MinVerticalAngle { get; private set; } = -90f;

    [Export(PropertyHint.Range, "0.0, 90.0, 0.1")]
    public float MaxVerticalAngle { get; private set; } = 45f;

    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MinZoom { get; private set; } = 2f;
    
    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MaxZoom { get; private set; } = 12f;
    
    [Export(PropertyHint.ResourceType, "PlayerCameraSettings")] 
    public PlayerCameraSettings Settings { get; private set; }
    
    #endregion

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>(() => null);
    
    public PlayerCameraLogic CameraLogic { get; private set; }
    public PlayerCameraLogic.IBinding CameraBinding { get; private set; }
    
    public void OnResolved()
    {
        CameraLogic = new PlayerCameraLogic();
        CameraLogic.Set(GameRepo);
        CameraLogic.Set(Settings);

        CameraBinding = CameraLogic.Bind();
        CameraBinding.Handle((in PlayerCameraLogic.Output.RotationComputed output) => OnRotationComputed(output.Rotation));
        
        CameraLogic.Start();
    }

    public override void _Input(InputEvent @event)
    {
        CameraLogic.Input(new PlayerCameraLogic.Input.OnInputEvent(@event));
    }

    private void OnRotationComputed(Vector3 cameraRotation)
    {
        var playerCamera = GameRepo.PlayerPhantomCamera.Value;
        playerCamera?.SetThirdPersonRotation(cameraRotation);
    }
    
}
