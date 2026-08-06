namespace RescueDrone;

using Godot;

public class VoxelNode
{
    public Vector3 Position { get; private set; }
    public float Size { get; private set; }
    
    public bool IsLeaf { get; set; }
    public bool IsEmpty { get; set; }
    public VoxelNode[] Children { get; set; }
    
    public VoxelNode(Vector3 position, float size)
    {
        Position = position;
        Size = size;
    }
}
