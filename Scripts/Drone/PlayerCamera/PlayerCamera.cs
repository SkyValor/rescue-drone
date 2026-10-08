namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class PlayerCamera : Node
{
    public override void _Notification(int what) => this.Notify(what);

    [Export(PropertyHint.ResourceType, "PlayerCameraSettings")]
    public PlayerCameraSettings Settings { get; private set; }

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    [Dependency] private IDroneRepo DroneRepo => this.DependOn<IDroneRepo>();
    
    public PlayerCameraLogic CameraLogic { get; private set; }
    public PlayerCameraLogic.IBinding CameraBinding { get; private set; }
    
    public void OnResolved()
    {
        CameraLogic = new PlayerCameraLogic();
        CameraLogic.Set(DroneRepo.InputComponent.Value);
        CameraLogic.Set(GameRepo);
        CameraLogic.Set(Settings);

        CameraBinding = CameraLogic.Bind();
        CameraBinding.Handle((in PlayerCameraLogic.Output.RotationComputed output) => OnRotationComputed(output.Rotation));
        CameraBinding.Handle((in PlayerCameraLogic.Output.ZoomComputed output) => OnZoomComputed(output.Length));
        
        CameraLogic.Start();
        SetPhysicsProcess(true);
    }

    public void OnPhysicsProcess(double delta)
    {
        CameraLogic.Input(new PlayerCameraLogic.Input.OnPhysicsTick());
    }

    public void OnExitTree()
    {
        CameraLogic.Stop();
        CameraBinding.Dispose();
    }

    private void OnRotationComputed(Vector3 cameraRotation)
    {
        var playerCamera = GameRepo?.PlayerPhantomCamera.Value;
        playerCamera?.SetThirdPersonRotation(cameraRotation);
    }

    private void OnZoomComputed(float zoom)
    {
        var playerCamera = GameRepo?.PlayerPhantomCamera.Value;
        if (playerCamera is null) return;
        
        playerCamera.SpringLength = zoom;
    }
    
}
