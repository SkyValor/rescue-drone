namespace RescueDrone;

using System;
using Chickensoft.Sync.Primitives;

public interface IGameRepo : IDisposable
{
    IAutoValue<PlayerMover> Player { get; }
    IAutoValue<WaypointCircuit[]> WaypointCircuits { get; }
    IAutoValue<OctreeGeneratorGroup> OctreeGenerator { get; }
    
    void SetPlayer(PlayerMover player);
    void SetWaypointCircuits(WaypointCircuit[] circuits);
    void SetOctreeGenerator(OctreeGeneratorGroup generator);
}

public class GameRepo : IGameRepo
{
    public IAutoValue<PlayerMover> Player => player;
    private readonly AutoValue<PlayerMover> player = new(null);

    public IAutoValue<WaypointCircuit[]> WaypointCircuits => waypointCircuits;
    private readonly AutoValue<WaypointCircuit[]> waypointCircuits = new(null);

    public IAutoValue<OctreeGeneratorGroup> OctreeGenerator => octreeGenerator;
    private readonly AutoValue<OctreeGeneratorGroup> octreeGenerator = new(null);

    private bool disposingValue;

    public void SetPlayer(PlayerMover player) => this.player.Value = player;

    public void SetWaypointCircuits(WaypointCircuit[] circuits) =>
        waypointCircuits.Value = circuits;

    public void SetOctreeGenerator(OctreeGeneratorGroup generator) =>
        octreeGenerator.Value = generator;

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposingValue) return;

        if (disposing)
        {
            // Dispose managed objects.
            player.Dispose();
            waypointCircuits.Dispose();
            octreeGenerator.Dispose();
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
