namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// TODO: Update the PlayerLogic and PlayerCameraLogic to use the new approach of reaching the InputComponent!!

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class InputComponent : Node
{
    public override void _Notification(int what) => this.Notify(what);
    
    public enum CameraZoomType { ZoomIn, ZoomOut }
    
    public event Action<CameraZoomType> CameraZoomInput;
    public event Action<Vector2> CameraRotationInput;
    
    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    
    public Vector3 MoveDirectionInput => new(HorizontalInput.X, VerticalInput, HorizontalInput.Y);
    public Vector2 HorizontalInput { get; set; }
    public float VerticalInput { get; set; }

    private Vector2 lastMouseMotion = Vector2.Zero;

    public void ToggleComponent(bool enabled)
    {
        SetProcessInput(enabled);
        SetProcessUnhandledInput(enabled);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return;
        lastMouseMotion = mouseMotion.Relative;
    }

    public void OnReady()
    {
        SetPhysicsProcess(true);
    }

    public void OnPhysicsProcess(double delta)
    {
        // We only reset the last mouse motion in idle time because
        // it might be requested during a physics frame
        CallDeferred(MethodName.ResetLastMouseMotion);
    }

    private void ResetLastMouseMotion()
    {
        lastMouseMotion = Vector2.Zero;
    }

    /// <summary>
    /// Get the current input from the user for the directional movement.
    /// Should be called from <see cref="Node._PhysicsProcess(double)"/>.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetDirectionalInput()
    {
        return IsCurrentDeviceTypeKeyboard()
            ? Input.GetVector(
            GameInputs.KbMoveLeft, GameInputs.KbMoveRight, 
            GameInputs.KbMoveForward, GameInputs.KbMoveBack)
            : Input.GetVector(
            GameInputs.JoyMoveLeft, GameInputs.JoyMoveRight, 
            GameInputs.JoyMoveForward, GameInputs.JoyMoveBack);
    }

    /// <summary>
    /// Get the current input from the user for the vertical movement (ascending or descending).
    /// Should be called from <see cref="Node._PhysicsProcess(double)"/>.
    /// </summary>
    /// <returns></returns>
    public float GetVerticalInput()
    {
        return IsCurrentDeviceTypeKeyboard()
            ? Input.GetAxis(GameInputs.KbDescend, GameInputs.KbAscend) 
            : Input.GetAxis(GameInputs.JoyDescend, GameInputs.JoyAscend);
    }

    /// <summary>
    /// Get the current input from the user for the camera zoom in/out.
    /// </summary>
    /// <returns></returns>
    public float GetCameraZoomInput()
    {
        if (IsCurrentDeviceTypeKeyboard())
        {
            return DoesCurrentSchemeIncludeMouse()
                ? Input.GetAxis(GameInputs.MouseCamZoomOut, GameInputs.MouseCamZoomIn)
                : Input.GetAxis(GameInputs.KbCamZoomOut, GameInputs.KbCamZoomIn);
        }
        
        return Input.GetAxis(GameInputs.JoypadCamZoomOut, GameInputs.JoypadCamZoomIn);
    }

    /// <summary>
    /// Get the current input from the user for the camera rotation.
    /// Should be called from <see cref="Node._PhysicsProcess(double)"/>.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetCameraRotationInput()
    {
        if (IsCurrentDeviceTypeKeyboard())
        {
            return DoesCurrentSchemeIncludeMouse()
                ? lastMouseMotion
                : Input.GetVector(
                    GameInputs.KbCamRotateLeft, GameInputs.KbCamRotateRight, 
                    GameInputs.KbCamRotateDown, GameInputs.KbCamRotateUp);
        }
        
        return Input.GetVector(
            GameInputs.JoyCamRotateLeft, GameInputs.JoyCamRotateRight,
            GameInputs.JoyCamRotateDown, GameInputs.JoyCamRotateUp);
    }

    public virtual void PhysicsMovementUpdate()
    {
        
    }

    private bool IsCurrentDeviceTypeKeyboard() 
        => GameRepo.DeviceHandler.Value.CurrentDeviceType.Value is InputDeviceType.Computer;
    private bool DoesCurrentSchemeIncludeMouse() 
        => GameRepo.DeviceHandler.Value.CurrentDeviceScheme.Value is InputDeviceScheme.KeyboardAndMouse;

    protected void InvokeCameraZoomInput(CameraZoomType cameraZoom) => CameraZoomInput?.Invoke(cameraZoom);
    protected void InvokeCameraRotationInput(Vector2 cameraRelative) => CameraRotationInput?.Invoke(cameraRelative);
    
}
