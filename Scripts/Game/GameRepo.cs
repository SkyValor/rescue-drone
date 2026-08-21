namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using PhantomCamera;

public interface IGameRepo : IDisposable
{
    event Action LevelStart;
    
    IAutoValue<PlayerMover> Player { get; }
    IAutoValue<PhantomCamera3D> PlayerPhantomCamera { get; }
    IAutoValue<Camera3D> MainCamera { get; }
    IAutoValue<SparseVoxelOctree> SVO { get; }
    IAutoValue<WaypointCircuit[]> WaypointCircuits { get; }
    
    IAutoValue<bool> PlayerInControl { get; }

    void InvokeLevelStart();
    
    void SetPlayer(PlayerMover player);
    void SetPlayerPhantomCamera(PhantomCamera3D camera);
    void SetMainCamera(Camera3D camera);
    void SetSVO(SparseVoxelOctree tree);
    void SetWaypointCircuits(WaypointCircuit[] circuits);

    void SetPlayerInControl(bool inControl);
}

public class GameRepo : IGameRepo
{
    public event Action LevelStart;
    
    public IAutoValue<PlayerMover> Player => player;
    private readonly AutoValue<PlayerMover> player = new(null);

    public IAutoValue<PhantomCamera3D> PlayerPhantomCamera => playerCamera;
    private readonly AutoValue<PhantomCamera3D> playerCamera = new(null);
    
    public IAutoValue<Camera3D> MainCamera => mainCamera;
    private readonly AutoValue<Camera3D> mainCamera = new(null);
    
    public IAutoValue<SparseVoxelOctree> SVO => svOctree;
    private readonly AutoValue<SparseVoxelOctree> svOctree = new(null);

    public IAutoValue<WaypointCircuit[]> WaypointCircuits => waypointCircuits;
    private readonly AutoValue<WaypointCircuit[]> waypointCircuits = new(null);
    
    public IAutoValue<bool> PlayerInControl => playerInControl;
    private readonly AutoValue<bool> playerInControl = new(false);

    private bool disposingValue;

    public void InvokeLevelStart() => LevelStart?.Invoke();

    public void SetPlayer(PlayerMover player) => this.player.Value = player;
    public void SetPlayerPhantomCamera(PhantomCamera3D camera) => playerCamera.Value = camera;
    public void SetMainCamera(Camera3D camera) => mainCamera.Value = camera;
    public void SetSVO(SparseVoxelOctree tree) => svOctree.Value = tree;
    public void SetWaypointCircuits(WaypointCircuit[] circuits) => waypointCircuits.Value = circuits;
    public void SetPlayerInControl(bool inControl) => playerInControl.Value = inControl;

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            // Dispose managed objects.
            player.Dispose();
            playerCamera.Dispose();
            svOctree.Dispose();
            waypointCircuits.Dispose();
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
