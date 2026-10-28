namespace RescueDrone;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

/**
 *  1. Disconnect Origin from its neighbors
 *  2. Tree removes this Origin from its EmptyLeaves
 *  3. The AutoList.Binding OnRemove callback is called
 *  4. Have Origin subdivide
 *  5. For each of his children, connect with neighbors
 */

[Tool]
[Meta(typeof(IAutoOn))]
public partial class SparseVoxelOctree : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    private readonly Color DebugColorTree = new(Colors.Green, 0.4f);

    public event Action EarlyGenerationStarted;
    public event Action EarlyGenerationFinished;
    public event Action<VoxelNode, VoxelNode[]> OnNodeSubdivided;

    [Export] private float RootSize { get; set; } = 32f;
    [Export] private int InitialSubdivisions { get; set; } = 4;
    [Export] private float MinNodeSize { get; set; } = 1f;
    
    [Export] private bool DebugTree { get; set; }
    [Export] private bool DebugTreeLeaves { get; set; }
    
    public VoxelNode Root { get; private set; }

    public void OnReady()
    {
        VoxelNode.OnNodeSubdivided += OnVoxelNodeSubdivided;
    }

    public void OnExitTree()
    {
        VoxelNode.OnNodeSubdivided -= OnVoxelNodeSubdivided;
    }

    public void OnProcess(double delta)
    {
        if (DebugTree) DebugDraw3D.DrawBox(GlobalPosition, Quaternion.Identity, Vector3.One * RootSize, DebugColorTree);
    }

    public void GenerateOctree()
    {
        EarlyGenerationStarted?.Invoke();

        Root = new VoxelNode(GlobalPosition, RootSize, null);
        Root.Subdivide(GetWorld3D(), MinNodeSize, InitialSubdivisions);
        
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
    
    // private void FindEmptyLeavesRecursive(VoxelNode currentNode)
    // {
    //     if (currentNode.IsLeaf)
    //     {
    //         if (currentNode.IsEmpty)
    //             EmptyLeaves.Add(currentNode);
    //
    //         return;
    //     }
    //
    //     foreach (var childNode in currentNode.Children)
    //         FindEmptyLeavesRecursive(childNode);
    // }

    // public VoxelNode GetClosestEmptyLeaf(Vector3 toPosition)
    // {
    //     VoxelNode closestNode = null;
    //     float distance = float.MaxValue;
    //     foreach (var leaf in EmptyLeaves)
    //     {
    //         if (leaf.Position.DistanceSquaredTo(toPosition) >= distance)
    //             continue;
    //         
    //         closestNode = leaf;
    //         distance = leaf.Position.DistanceSquaredTo(toPosition);
    //     }
    //
    //     return closestNode;
    // }
    
    // /// <summary>
    // /// Get the leaf voxel node which contains the given position.
    // /// </summary>
    // /// <param name="position"></param>
    // /// <returns></returns>
    // public VoxelNode GetLeafAtPosition(Vector3 position)
    // {
    //     return Root.ContainsPoint(position) ? FindLeafRecursive(Root, position) : null;
    // }

    // private static VoxelNode FindLeafRecursive(VoxelNode currentNode, Vector3 position)
    // {
    //     if (currentNode.IsLeaf) return currentNode;
    //
    //     return (
    //             from child in currentNode.Children 
    //             where child is not null && child.ContainsPoint(position) 
    //             select FindLeafRecursive(child, position))
    //         .FirstOrDefault();
    // }

    public void DrawVoxelOctree()
    {
        Root.DrawVoxelWireframe();
    }
    
}
