namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;

public interface IGameRepo : IDisposable
{
    IAutoValue<PlayerMover> Player { get; }
    IAutoValue<WaypointCircuit[]> WaypointCircuits { get; }
    IAutoValue<SparseVoxelOctreeShape> SVOctree { get; }
    
    void SetPlayer(PlayerMover player);
    void SetWaypointCircuits(WaypointCircuit[] circuits);
    void SetSVOctree(SparseVoxelOctreeShape tree);
}

public class GameRepo : IGameRepo
{
    public IAutoValue<PlayerMover> Player => player;
    private readonly AutoValue<PlayerMover> player = new(null);

    public IAutoValue<WaypointCircuit[]> WaypointCircuits => waypointCircuits;
    private readonly AutoValue<WaypointCircuit[]> waypointCircuits = new(null);

    public IAutoValue<SparseVoxelOctreeShape> SVOctree => svOctree;
    private readonly AutoValue<SparseVoxelOctreeShape> svOctree = new(null);

    private bool disposingValue;

    public void SetPlayer(PlayerMover player) => this.player.Value = player;
    public void SetSVOctree(SparseVoxelOctreeShape tree) => svOctree.Value = tree;
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
            waypointCircuits.Dispose();
            svOctree.Dispose();
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
