namespace RescueDrone;

using Godot;

public partial class OctreeGeneratorGroup : Node3D
{
    [Export] public Node3D GroupObjects { get; private set; }
    [Export] public float MinNodeSize { get; private set; }
    
    public SparseVoxelOctree Tree { get; private set; }

    public readonly AStarGraph Graph = new();

    public override void _EnterTree()
    {
        var objects = new Node3D[GroupObjects.GetChildCount()];
        for (int i = 0; i < objects.Length; i++)
            objects[i] = (Node3D) GroupObjects.GetChild(i);
        Tree = new SparseVoxelOctree(objects, MinNodeSize, Graph);
    }

    public override void _Process(double delta)
    {
        Tree.DrawTree();
    }
}
