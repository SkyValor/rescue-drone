namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Tool]
[Meta(typeof(IAutoOn))]
public partial class SparseVoxelOctree : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    private readonly Color DebugColorTree = new(Colors.Green, 0.4f);
    private readonly Color DebugColorRootBranch = new(Colors.White, 0.6f);

    public event System.Action<VoxelNode, VoxelNode[]> OnNodeSubdivided; 

    [Export] private float RootSize { get; set; } = 32f;
    [Export] private int InitialSubdivisions { get; set; } = 4;
    [Export] private float MinNodeSize { get; set; } = 1f;
    
    [Export] private bool DebugTree { get; set; }
    [Export] private bool DebugTreeLeaves { get; set; }

    [ExportCategory("Flight Boundaries")]
    [Export] public float UpperSkyLimit { get; private set; } = 120f;
    [Export] public float LowerGroundLimit { get; private set; }
    
    public VoxelNode Root { get; private set; }
    public bool IsReadyForPathfinding { get; private set; }

    public void OnReady()
    {
        VoxelNode.OnNodeSubdivided += OnVoxelNodeSubdivided;
    }

    public void OnExitTree()
    {
        VoxelNode.OnNodeSubdivided -= OnVoxelNodeSubdivided;
    }

    public override void _Process(double delta)
    {
        if (!DebugTree) return;

        if (Engine.IsEditorHint())
        {
            DebugDraw3D.DrawBox(GlobalPosition, Quaternion.Identity, Vector3.One * RootSize, DebugColorTree, true);
            
            var halfSize = RootSize * 0.5f;
            var quarterSize = halfSize * 0.5f;
            var offsets = VoxelNode.GetOffsetPositionArray(quarterSize);
            foreach (var direction in offsets)
                DebugDraw3D.DrawBox(GlobalPosition + direction, Quaternion.Identity, Vector3.One * halfSize, DebugColorRootBranch, true);
        }
        else
            Root?.DrawVoxelWireframe();
    }

    public async void GenerateOctreeAsync()
    {
        var world = GetWorld3D();
        if (world is null)
        {
            DebugConsole.PrintError("SparseVoxelOctree: World3D could not be found!");
            return;
        }

        Root = new VoxelNode(GlobalPosition, RootSize, null);
        await Root.SubdivideAsync(world, MinNodeSize, InitialSubdivisions, UpperSkyLimit, LowerGroundLimit, this);

        IsReadyForPathfinding = true;
        DebugConsole.Print("\"SparseVoxelOctree async generation complete. Ready for drone pathfinding!\"");
    }
    
    public void DrawVoxelOctree()
    {
        Root?.DrawVoxelWireframe();
    }
    
    /// <summary>
    /// Returns a list of all leaf nodes that are empty (passable for drones).
    /// </summary>
    public List<VoxelNode> GetNavigableWaypoints()
    {
        var waypoints = new List<VoxelNode>();
        Root?.GetEmptyLeafNodes(waypoints);
        return waypoints;
    }

    /// <summary>
    /// Gets the VoxelNode at a given world position.
    /// </summary>
    public VoxelNode GetNodeAtPosition(Vector3 position)
    {
        return Root?.FindDeepestExistingNode(position);
    }

    /// <summary>
    /// Gets or dynamically subdivides to the most precise VoxelNode at a given world position.
    /// </summary>
    public VoxelNode GetNodeAtPositionPrecise(Vector3 position)
    {
        return Root?.FindOrCreateAtPosition(position, MinNodeSize, UpperSkyLimit, LowerGroundLimit, GetWorld3D());
    }

    private void OnVoxelNodeSubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        OnNodeSubdivided?.Invoke(parentNode, octants);
    }
    
}
