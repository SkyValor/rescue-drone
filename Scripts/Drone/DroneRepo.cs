namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;
using Godot;

public interface IDroneRepo : IDisposable
{
    event Action DroneStartedMoving;
    event Action DroneStoppedMoving;
    event Action<Vector2> InputReceived;
    
    IAutoValue<IFlyingDrone> FlyingDrone { get; }
    IAutoValue<Node3D> DroneModel { get; }
    IAutoValue<InputComponent> InputComponent { get; }
    
    void InvokeInputReceived(Vector2 tiltDirection);
    
    void SetFlyingDrone(IFlyingDrone drone);
    void SetDroneModel(Node3D model);
    void SetInputComponent(InputComponent inputComponent);
}

public class DroneRepo : IDroneRepo
{
    public event Action DroneStartedMoving;
    public event Action DroneStoppedMoving;
    public event Action<Vector2> InputReceived;

    public IAutoValue<IFlyingDrone> FlyingDrone => flyingDrone;
    private readonly AutoValue<IFlyingDrone> flyingDrone = new(null);
    
    public IAutoValue<Node3D> DroneModel => droneModel;
    private readonly AutoValue<Node3D> droneModel = new(null);
    
    public IAutoValue<InputComponent> InputComponent => input;
    private readonly AutoValue<InputComponent> input = new(null);

    public void InvokeDroneStartedMoving() => DroneStartedMoving?.Invoke();
    public void InvokeDroneStoppedMoving() => DroneStoppedMoving?.Invoke();
    public void InvokeInputReceived(Vector2 tiltDirection) => InputReceived?.Invoke(tiltDirection);

    public void SetFlyingDrone(IFlyingDrone drone) => flyingDrone.Value = drone;
    public void SetDroneModel(Node3D model) => droneModel.Value = model;
    public void SetInputComponent(InputComponent inputComponent) => input.Value = inputComponent;

    private bool disposingValue;
    
    #region Internals
    public void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            flyingDrone.Dispose();
            droneModel.Dispose();
            input.Dispose();
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
