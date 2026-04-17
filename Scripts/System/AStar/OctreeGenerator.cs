namespace RescueDrone;

using Godot;

public partial class OctreeGenerator : Node
{
    [Export] public Node3D[] Objects { get; private set; }
    [Export] public float MinNodeSize { get; private set; } = 1f;

    public Octree Tree { get; private set; }
    
    public readonly AStarGraph Graph = new();

    public override void _EnterTree()
    {
        Tree = new Octree(Objects, MinNodeSize, Graph);
    }

    public override void _Process(double delta)
    {
        Tree.DrawTree();
    }
    
}
