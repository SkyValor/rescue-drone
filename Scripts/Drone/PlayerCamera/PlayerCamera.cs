namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

[Meta(typeof(IAutoNode))]
public partial class PlayerCamera : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    [Export(PropertyHint.ResourceType, "PlayerCameraSettings")]
    public PlayerCameraSettings Settings { get; private set; }

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
        CameraBinding.Handle((in PlayerCameraLogic.Output.ZoomComputed output) => OnZoomComputed(output.Length));
        
        CameraLogic.Start();
    }

    public override void _Process(double delta)
    {
        CameraLogic.Input(new PlayerCameraLogic.Input.OnProcessTick(delta));
    }

    // public void OnProcess(double delta)
    // {
    //     CameraLogic.Input(new PlayerCameraLogic.Input.OnProcessTick(delta));
    // }

    public void OnExitTree()
    {
        CameraLogic.Stop();
        CameraBinding.Dispose();
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

    private void OnZoomComputed(float zoom)
    {
        var playerCamera = GameRepo.PlayerPhantomCamera.Value;
        if (playerCamera is null) return;
        
        playerCamera.SpringLength = zoom;
    }
    
}
