namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using PhantomCamera;

public interface IUserDroneController : ICharacterBody3D
{
    
}

[Meta(typeof(IAutoNode))]
public partial class UserDroneController : CharacterBody3D, IUserDroneController
{
    public override void _Notification(int what) => this.Notify(what);

    [ExportGroup("Camera Settings")]
    [Export] public float CameraRotationSpeed { get; private set; } = 3f;

    [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    
    private PhantomCamera3D pCamera;

    public void OnReady()
    {
        pCamera = GetNode<Node3D>("../PlayerThirdPersonCamera").AsPhantomCamera3D();
        SetProcessUnhandledInput(false);
    }
    
    public void OnResolved()
    {
        
    }
    
}
