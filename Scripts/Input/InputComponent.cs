namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class InputComponent : Node
{
    public override void _Notification(int what) => this.Notify(what);
    
    public enum CameraZoomType { ZoomIn, ZoomOut }
    
    public event Action<CameraZoomType> CameraZoomInput;
    public event Action<Vector2> CameraRotationInput;
    
    public Vector3 MoveDirectionInput => new(HorizontalInput.X, VerticalInput, HorizontalInput.Y);
    public Vector2 HorizontalInput { get; set; }
    public float VerticalInput { get; set; }
    
    [Dependency] private IGameRepo GameRepo { get; set; }

    public void OnResolved()
    {
        
    }

    public void ToggleComponent(bool enabled)
    {
        SetProcessInput(enabled);
        SetProcessUnhandledInput(enabled);
    }
    
    public virtual void PhysicsMovementUpdate() { }

    protected void InvokeCameraZoomInput(CameraZoomType cameraZoom) => CameraZoomInput?.Invoke(cameraZoom);
    protected void InvokeCameraRotationInput(Vector2 cameraRelative) =>  CameraRotationInput?.Invoke(cameraRelative);
    
}
