namespace RescueDrone.Scripts;

using Godot;

public partial class OctreeGenerator : Node
{
    [Export] public Node3D[] Objects { get; private set; }
    [Export] public float MinNodeSize { get; private set; } = 1f;

    public readonly Graph Waypoints = new();
    
    private Octree tree;

    public override void _Ready()
    {
        tree = new Octree(Objects, MinNodeSize, Waypoints);
    }

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawBox(tree.Bounds.GetCenter(), Quaternion.Identity, tree.Bounds.Size, Colors.Green, true);
        tree.Root.DrawNode();
        tree.Graph.DrawGraph();
    }
}
