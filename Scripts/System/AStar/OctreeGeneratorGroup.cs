namespace RescueDrone;

using Chickensoft.GodotNodeInterfaces;
using Godot;

public interface IOctreeGenerator : INode3D
{
    
}

public partial class OctreeGeneratorGroup : Node3D, IOctreeGenerator
{
    [Export] public Node3D GroupObjects { get; private set; }
    [Export] public float MinNodeSize { get; private set; }
    [Export] public bool SubdivideMax { get; private set; }
    
    public SparseVoxelOctreeShape Tree { get; private set; }

    public override void _Ready()
    {
        Tree = new SparseVoxelOctreeShape(GlobalPosition, Scale, MinNodeSize, GetWorld3D(), SubdivideMax);
    }

    public override void _Process(double delta)
    {
        // Tree.DrawTree();
    }
}
