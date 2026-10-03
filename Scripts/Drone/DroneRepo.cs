namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;
using Godot;

public interface IDroneRepo : IDisposable
{
    event Action HoverBobStarted;
    event Action HoverBobStopped;
    event Action<Vector2> TiltChanged;
    
    IAutoValue<Node3D> DroneModel { get; }
    
    void InvokeHoverBobStarted();
    void InvokeHoverBobStopped();
    void InvokeTiltChanged(Vector2 tiltDirection);
    
    void SetDroneModel(Node3D model);
}

public class DroneRepo : IDroneRepo
{
    public event Action HoverBobStarted;
    public event Action HoverBobStopped;
    public event Action<Vector2> TiltChanged;

    public IAutoValue<Node3D> DroneModel => droneModel;
    private readonly AutoValue<Node3D> droneModel = new(null);

    public void InvokeHoverBobStarted() => HoverBobStarted?.Invoke();
    public void InvokeHoverBobStopped() => HoverBobStopped?.Invoke();
    public void InvokeTiltChanged(Vector2 tiltDirection) => TiltChanged?.Invoke(tiltDirection);

    public void SetDroneModel(Node3D model) => droneModel.Value = model;

    private bool disposingValue;
    
    #region Internals
    public void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            droneModel.Dispose();
        }
        
        disposingValue = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
    
}
