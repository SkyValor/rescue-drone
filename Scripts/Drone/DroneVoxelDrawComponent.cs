namespace RescueDrone;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;

/// <summary>
/// Provides voxel node drawing in real time by creating a wireframe box on the current deepest voxel node
/// and the immediate connections to it.
/// </summary>
/// <remarks>This is a component to be attached on a moving object in the scene. For debugging purposes only.</remarks>
[Meta(typeof(IDependent), typeof(IAutoOn))]
public partial class DroneVoxelDrawComponent : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>(() => null);

    [Export] private bool DebugCurrentNode { get; set; }
    [Export] private bool DebugNodeConnections { get; set; }
    
    private IPathfindSVO pathfinder;
    private SparseVoxelOctree octree;
    private AutoValue<SparseVoxelOctree>.Binding svoBind;
    private List<VoxelNode> navigables = [];

    public void OnResolved()
    {
        // Either wait for the SVO reference or setup it now
        var svo = GameRepo.SVO.Value;
        if (svo is null)
        {
            svoBind = GameRepo.SVO.Bind();
            svoBind.OnValue(SetupPathfinder);
        }
        else
        {
            SetupPathfinder(svo);
        }
    }

    public void OnExitTree()
    {
        svoBind?.Dispose();
        if (octree is not null)
            octree.OnNodeSubdivided -= OnNodeSubdivided;
    }

    public void OnPhysicsProcess(double delta)
    {
        DrawVoxelOctreeConnections(GlobalPosition);
    }
    
    /// <summary>
    /// Get the deepest existing voxel node in the current world position and draw a wireframe box in
    /// dark magenta for it. Then, draw a wireframe box in goldenrod for every voxel node connected to it.
    /// </summary>
    private void DrawVoxelOctreeConnections(Vector3 position)
    {
        var svo = GameRepo.SVO.Value;
        if (svo is null) return;

        if (!DebugCurrentNode) return;
        
        var centralNode = svo.GetNodeAtPosition(position);
        if (centralNode is null || !pathfinder.IsPointContained(centralNode.Id))
        {
            DebugConsole.Print("Cannot get point for current position.");
            return;
        }
        
        centralNode.DrawWithColor(Colors.DarkMagenta);
        if (!DebugNodeConnections) return;
        
        foreach (var connectionId in pathfinder.GetPointConnections(centralNode.Id))
        {
            var connectedNode = navigables.FirstOrDefault(nav => nav.Id == connectionId);
            if (connectedNode is null)
            {
                DebugConsole.Print("Navigable list does not contain node with correct ID.");
                continue;
            }
            
            connectedNode.DrawWithColor(Colors.Goldenrod);
        }
    }

    private void SetupPathfinder(SparseVoxelOctree octree)
    {
        if (octree is null) return;
        
        svoBind?.Dispose();
        pathfinder = new VoxelOctreeAStar(octree);
        navigables = octree.GetNavigableWaypoints();

        this.octree = octree;
        octree.OnNodeSubdivided += OnNodeSubdivided;
        SetPhysicsProcess(true);
    }

    private void OnNodeSubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        navigables.Remove(parentNode);
        navigables.AddRange(octants);
    }
    
}
