namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;

public interface IGameRepo : IDisposable
{
    IAutoValue<PlayerMover> Player { get; }
    IAutoValue<SparseVoxelOctree> SVO { get; }
    IAutoValue<WaypointCircuit[]> WaypointCircuits { get; }
    
    void SetPlayer(PlayerMover player);
    void SetSVO(SparseVoxelOctree tree);
    void SetWaypointCircuits(WaypointCircuit[] circuits);
}

public class GameRepo : IGameRepo
{
    public IAutoValue<PlayerMover> Player => player;
    private readonly AutoValue<PlayerMover> player = new(null);
    
    public IAutoValue<SparseVoxelOctree> SVO => svOctree;
    private readonly AutoValue<SparseVoxelOctree> svOctree = new(null);

    public IAutoValue<WaypointCircuit[]> WaypointCircuits => waypointCircuits;
    private readonly AutoValue<WaypointCircuit[]> waypointCircuits = new(null);

    private bool disposingValue;

    public void SetPlayer(PlayerMover player) => this.player.Value = player;
    public void SetSVO(SparseVoxelOctree tree) => svOctree.Value = tree;
    public void SetWaypointCircuits(WaypointCircuit[] circuits) =>
        waypointCircuits.Value = circuits;

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            // Dispose managed objects.
            player.Dispose();
            svOctree.Dispose();
            waypointCircuits.Dispose();
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
