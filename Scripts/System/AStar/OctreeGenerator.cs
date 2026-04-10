namespace RescueDrone;

using Godot;

public partial class OctreeGenerator : Node
{
    [Export] public Node3D[] Objects { get; private set; }
    [Export] public float MinNodeSize { get; private set; } = 1f;

    public Octree Tree { get; private set; }
    
    public readonly Graph Waypoints = new();

    // public override void _Ready()
    // {
    //     
    // }

    public override void _EnterTree()
    {
        Tree = new Octree(Objects, MinNodeSize, Waypoints);
    }

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawBox(Tree.Bounds.GetCenter(), Quaternion.Identity, Tree.Bounds.Size, Colors.Green, true);
        Tree.Root.DrawNode();
        Tree.Graph.DrawGraph();
    }
    
}
