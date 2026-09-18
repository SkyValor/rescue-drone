namespace RescueDrone;

using System;
using Godot;

public partial class InputComponent : Node
{
    public enum CameraZoomType { ZoomIn, ZoomOut}
    
    public event Action<Vector2> OnHorizontalInput;
    public event Action<Vector3> OnMoveInput;
    public event Action<Vector2> CameraRotationInputChanged;
    
    public event Action<CameraZoomType> CameraZoomInput;
    public event Action<Vector2> CameraRotationInput;
    
    public Vector3 MoveDirectionInput => new(HorizontalInput.X, VerticalInput, HorizontalInput.Y);
    public Vector2 HorizontalInput { get; set; }
    public float VerticalInput { get; set; }

    public void ToggleComponent(bool enabled)
    {
        SetProcessInput(enabled);
    }
    
    public virtual void PhysicsMovementUpdate() { }

    protected void InvokeCameraZoomInput(CameraZoomType cameraZoom) => CameraZoomInput?.Invoke(cameraZoom);
    protected void InvokeCameraRotationInput(Vector2 cameraRelative) =>  CameraRotationInput?.Invoke(cameraRelative);
    
}
