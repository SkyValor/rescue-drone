namespace RescueDrone;

using System;
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

    public event Action EarlyGenerationStarted;
    public event Action EarlyGenerationFinished;
    public event Action<VoxelNode, VoxelNode[]> OnNodeSubdivided;
    public event Action<VoxelNode, VoxelNode[]> OnNodePartiallySubdivided; 
    public event Action<VoxelNode, VoxelNode[]> OnNodeSubdividedForCollisions;

    [Export] private float RootSize { get; set; } = 32f;
    [Export] private int InitialSubdivisions { get; set; } = 4;
    [Export] private float MinNodeSize { get; set; } = 1f;
    
    [Export] private bool DebugTree { get; set; }
    [Export] private bool DebugTreeLeaves { get; set; }
    
    public VoxelNode Root { get; private set; }

    public void OnReady()
    {
        VoxelNode.OnNodeSubdivided += OnVoxelNodeSubdivided;
        VoxelNode.OnNodePartiallySubdivided += OnVoxelNodePartiallySubdivided;
        VoxelNode.OnNodeSubdividedForCollisions += OnVoxelNodeSubdividedForCollisions;
    }

    public void OnExitTree()
    {
        VoxelNode.OnNodeSubdivided -= OnVoxelNodeSubdivided;
        VoxelNode.OnNodePartiallySubdivided -= OnVoxelNodePartiallySubdivided;
        VoxelNode.OnNodeSubdividedForCollisions -= OnVoxelNodeSubdividedForCollisions;
    }

    public override void _Process(double delta)
    {
        if (!DebugTree) return;
        
        if (Engine.IsEditorHint())
            DebugDraw3D.DrawBox(GlobalPosition, Quaternion.Identity, Vector3.One * RootSize, DebugColorTree, true);
        else
            Root?.DrawVoxelWireframe();
    }

    public void GenerateOctree()
    {
        EarlyGenerationStarted?.Invoke();

        Root = new VoxelNode(GlobalPosition, RootSize, null);
        Root.SubdivideForCollisions(GetWorld3D(), MinNodeSize, InitialSubdivisions);
        
        DebugConsole.Print("SparseVoxelOctree generated successfully.");
        EarlyGenerationFinished?.Invoke();
    }

    public void GenerateOctreeInDepth()
    {
        EarlyGenerationStarted?.Invoke();
        
        Root = new VoxelNode(GlobalPosition, RootSize, null);
        Root.SubdivideInDepth(GetWorld3D(), MinNodeSize);
        
        DebugConsole.Print("SparseVoxelOctree generated successfully.");
        EarlyGenerationFinished?.Invoke();
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
        return Root?.FindOrCreateAtPosition(position, MinNodeSize, GetWorld3D());
    }

    private void OnVoxelNodeSubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        OnNodeSubdivided?.Invoke(parentNode, octants);
    }

    private void OnVoxelNodePartiallySubdivided(VoxelNode parentNode, VoxelNode[] octants)
    {
        OnNodePartiallySubdivided?.Invoke(parentNode, octants);
    }

    private void OnVoxelNodeSubdividedForCollisions(VoxelNode parentNode, VoxelNode[] octants)
    {
        OnNodeSubdividedForCollisions?.Invoke(parentNode, octants);
    }
    
}
