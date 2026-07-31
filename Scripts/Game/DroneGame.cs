namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

// TODO: Check if a Waypoint requires the SVO to subdivide until reaching its position, as if it's a physical object.
// This would benefit the accuracy of pathfinding.

[Meta(typeof(IAutoNode))]
public partial class DroneGame : Node
{
    public override void _Notification(int what) => this.Notify(what);

    [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    
    [Node] private IOctreeGenerator OctreeGenerator { get; set; }
    [Node] private IWaypointCircuit WaypointCircuits { get; set; }
    [Node] private INode3D PlayerSpawnPoint { get; set; }
    [Node] private INode3D EnemySpawnPoint { get; set; }

    private IGameRepo gameRepo;

    public void OnReady()
    {
        gameRepo = new GameRepo();
    }

    public void OnResolved()
    {
        
    }
}
