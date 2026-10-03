namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using PhantomCamera;

public interface IGameRepo : IDisposable
{
    event Action LevelStart;
    event Action LevelIntroStarted;
    event Action LevelIntroSkipped;
    event Action LevelIntroCompleted;

    event Action<SmallDrone> DronePickedUp;
    event Action<SmallDrone[]> DronesDelivered;
    
    IAutoValue<UserSettings> UserSettings { get; }
    IAutoValue<PlayerDrone> PlayerDrone { get; }
    IAutoValue<PhantomCamera3D> PlayerPhantomCamera { get; }
    IAutoValue<InputDeviceHandler> DeviceHandler { get; }
    IAutoValue<InputComponent> InputComponent { get; }
    IAutoValue<Camera3D> MainCamera { get; }
    IAutoValue<SparseVoxelOctree> SVO { get; }
    IAutoValue<WaypointCircuit[]> WaypointCircuits { get; }
    IAutoValue<EnemyAIDrone[]> EnemyDrones { get; }
    IAutoValue<bool> PlayerInControl { get; }

    void InvokeLevelStart();
    void InvokeLevelIntroStarted();
    void InvokeLevelIntroSkipped();
    void InvokeLevelIntroCompleted();
    
    void InvokeDronePickedUp(SmallDrone drone);
    void InvokeDronesDelivered(SmallDrone[] drones);
    
    void SetUserSettings(UserSettings settings);
    void SetPlayer(PlayerDrone player);
    void SetPlayerPhantomCamera(PhantomCamera3D camera);
    void SetDeviceHandler(InputDeviceHandler handler);
    void SetInputComponent(InputComponent component);
    void SetMainCamera(Camera3D camera);
    void SetSVO(SparseVoxelOctree tree);
    void SetWaypointCircuits(WaypointCircuit[] circuits);
    void SetEnemyDrones(EnemyAIDrone[] enemies);
    void SetPlayerInControl(bool inControl);
}

public class GameRepo : IGameRepo
{
    public event Action LevelStart;
    public event Action LevelIntroStarted;
    public event Action LevelIntroSkipped;
    public event Action LevelIntroCompleted;

    public event Action<SmallDrone> DronePickedUp;
    public event Action<SmallDrone[]> DronesDelivered;

    public IAutoValue<UserSettings> UserSettings => userSettings;
    private readonly AutoValue<UserSettings> userSettings = new(null);
    
    public IAutoValue<PlayerDrone> PlayerDrone => playerDrone;
    private readonly AutoValue<PlayerDrone> playerDrone = new(null);

    public IAutoValue<PhantomCamera3D> PlayerPhantomCamera => playerCamera;
    private readonly AutoValue<PhantomCamera3D> playerCamera = new(null);
    
    public IAutoValue<InputDeviceHandler> DeviceHandler => deviceHandler;
    private readonly AutoValue<InputDeviceHandler> deviceHandler = new(null);
    
    public IAutoValue<InputComponent> InputComponent => inputComponent;
    private readonly AutoValue<InputComponent> inputComponent = new(null);
    
    public IAutoValue<Camera3D> MainCamera => mainCamera;
    private readonly AutoValue<Camera3D> mainCamera = new(null);
    
    public IAutoValue<SparseVoxelOctree> SVO => svOctree;
    private readonly AutoValue<SparseVoxelOctree> svOctree = new(null);

    public IAutoValue<WaypointCircuit[]> WaypointCircuits => waypointCircuits;
    private readonly AutoValue<WaypointCircuit[]> waypointCircuits = new(null);

    public IAutoValue<EnemyAIDrone[]> EnemyDrones => enemyDrones;
    private readonly AutoValue<EnemyAIDrone[]> enemyDrones = new(null);
    
    public IAutoValue<bool> PlayerInControl => playerInControl;
    private readonly AutoValue<bool> playerInControl = new(false);

    private bool disposingValue;

    public void InvokeLevelStart() => LevelStart?.Invoke();
    public void InvokeLevelIntroStarted() => LevelIntroStarted?.Invoke();
    public void InvokeLevelIntroSkipped() => LevelIntroSkipped?.Invoke();
    public void InvokeLevelIntroCompleted() => LevelIntroCompleted?.Invoke();

    public void InvokeDronePickedUp(SmallDrone drone) => DronePickedUp?.Invoke(drone);
    public void InvokeDronesDelivered(SmallDrone[] drones) => DronesDelivered?.Invoke(drones);

    public void SetUserSettings(UserSettings settings) => userSettings.Value = settings;
    public void SetPlayer(PlayerDrone player) => playerDrone.Value = player;
    public void SetPlayerPhantomCamera(PhantomCamera3D camera) => playerCamera.Value = camera;
    public void SetDeviceHandler(InputDeviceHandler handler) => deviceHandler.Value = handler;
    public void SetInputComponent(InputComponent component) => inputComponent.Value = component;
    public void SetMainCamera(Camera3D camera) => mainCamera.Value = camera;
    public void SetSVO(SparseVoxelOctree tree) => svOctree.Value = tree;
    public void SetWaypointCircuits(WaypointCircuit[] circuits) => waypointCircuits.Value = circuits;
    public void SetEnemyDrones(EnemyAIDrone[] enemies) => enemyDrones.Value = enemies;
    public void SetPlayerInControl(bool inControl) => playerInControl.Value = inControl;

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            // Dispose managed objects.
            userSettings.Dispose();
            playerDrone.Dispose();
            playerCamera.Dispose();
            deviceHandler.Dispose();
            inputComponent.Dispose();
            mainCamera.Dispose();
            svOctree.Dispose();
            waypointCircuits.Dispose();
            enemyDrones.Dispose();
            playerInControl.Dispose();
        }

        disposingValue = true;
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
