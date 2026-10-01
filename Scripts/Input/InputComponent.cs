namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class InputComponent : Node, IDisposable
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
    
    private AutoValue<InputType>.Binding inputTypeBinding;
    private Vector2 lastMouseMotion = Vector2.Zero;

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return;
        lastMouseMotion = mouseMotion.Relative;
    }

    public void OnReady()
    {
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    public void OnResolved()
    {
        var deviceHandler = GameRepo.DeviceHandler.Value;
        inputTypeBinding = deviceHandler.CurrentInputType.Bind().OnValue(OnDeviceTypeChanged);
        
        OnDeviceTypeChanged(deviceHandler.CurrentInputType.Value);
    }

    public new void Dispose()
    {
        base.Dispose();
        inputTypeBinding.Dispose();
        GC.SuppressFinalize(this);
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

    private void OnDeviceTypeChanged(InputType inputType)
    {
        // We enable _Input and OnPhysicsProcess when mouse is enabled
        // so that we capture the mouse motion each frame
        
        var isMouseEnabled = inputType is InputType.KeyboardAndMouse;
        
        SetProcessInput(isMouseEnabled);
        SetPhysicsProcess(isMouseEnabled);
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
        // The mouse motion is relative to its previous position on the screen. The origin is the top-left, therefore
        // moving the cursor up will actually input a negative Y-axis value. To keep it consistent with other input types,
        // we invert the Y-axis on lastMouseMotion.
        if (IsCurrentDeviceTypeKeyboard())
        {
            return DoesCurrentSchemeIncludeMouse()
                ? lastMouseMotion * new Vector2(1, -1)
                : Input.GetVector(
                    GameInputs.KbCamRotateLeft, GameInputs.KbCamRotateRight, 
                    GameInputs.KbCamRotateDown, GameInputs.KbCamRotateUp);
        }
        
        return Input.GetVector(
            GameInputs.JoyCamRotateLeft, GameInputs.JoyCamRotateRight,
            GameInputs.JoyCamRotateDown, GameInputs.JoyCamRotateUp);
    }

    private bool IsCurrentDeviceTypeKeyboard() 
        => GameRepo.DeviceHandler.Value.CurrentInputType.Value is InputType.KeyboardAndMouse;
    private bool DoesCurrentSchemeIncludeMouse() 
        => GameRepo.DeviceHandler.Value.CurrentDeviceScheme.Value is InputDeviceScheme.KeyboardAndMouse;
    
}
