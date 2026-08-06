namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// TODO: Check if a Waypoint requires the SVO to subdivide until reaching its position, as if it's a physical object.
// This would benefit the accuracy of pathfinding.

// TODO: Load up these in order:
// 1. The map
// 2. Sparse Voxel Octree ramifications
// 3. Waypoint circuits, Player + Player camera, Enemies

[Meta(typeof(IAutoNode))]
public partial class DroneGame : Node3D, IProvide<IGameRepo>
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<float> ProgressChanged;
    public event Action LoadFinished;
    
    [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    
    [Node] private OctreeGeneratorGroup OctreeGenerator { get; set; }
    [Node] private WaypointCircuit WaypointCircuits { get; set; }
    [Node] private Node3D PlayerSpawnPoint { get; set; }
    [Node] private Node3D EnemySpawnPoint { get; set; }
    
    private IGameRepo GameRepo { get; set; }
    private GodotThread thread;

    IGameRepo IProvide<IGameRepo>.Value() => GameRepo;

    public void OnReady()
    {
        SetProcess(false);
        GameRepo = new GameRepo();
    }

    public void OnResolved()
    {
        thread.Start(Callable.From(MyBackgroundTask));
    }

    public void OnProcess(double delta)
    {
        
    }

    private void MyBackgroundTask()
    {
        OctreeGenerator.CreateSVOTree();
        GD.Print("Running in background.");
    }

    private void OnLoadingComplete()
    {
        // Call this method right after creating the SVO
        // but before placing player and enemies.
        this.Provide();
    }
    
}
