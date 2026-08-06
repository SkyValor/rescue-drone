namespace RescueDrone;

using Godot;

public partial class OctreeGeneratorGroup : Node3D
{
    [Export] public Node3D GroupObjects { get; private set; }
    [Export] public float MinNodeSize { get; private set; }
    [Export] public bool SubdivideMax { get; private set; }
    
    public SparseVoxelOctreeShape Tree { get; private set; }

    public void CreateSVOTree()
    {
        Tree = new SparseVoxelOctreeShape(GlobalPosition, Scale, MinNodeSize, GetWorld3D(), SubdivideMax);
    }

    public override void _Process(double delta)
    {
        // Tree.DrawTree();
    }
}
