namespace RescueDrone;

using Godot;

public class VoxelNode
{
    public readonly int Id;
    private static int nextId;
    
    public VoxelNode Parent { get; }
    public Vector3 Position { get; }
    public float Size { get; }

    public bool IsLeaf { get; set; }
    public bool IsEmpty { get; set; }
    public VoxelNode[] Children { get; set; }

    public VoxelNode(Vector3 position, float size, VoxelNode parent)
    {
        Id = nextId++;
        
        Position = position;
        Size = size;
        Parent = parent;
    }
    
    /// <summary>
    /// Draw a wireframe box of this voxel node and bubble down on its children. If this node is the root, it
    /// will be drawn in white. Only leaves are drawn. A leaf that is empty is drawn in green, otherwise in red.
    /// </summary>
    public void DrawVoxelWireframe()
    {
        var size = new Vector3(Size, Size, Size);
        
        if (Parent is null) DebugDraw3D.DrawBox(Position, Quaternion.Identity, size, Colors.White, true);
        if (IsLeaf)
        {
            var color = IsEmpty ? Colors.Green : Colors.Red;
            DebugDraw3D.DrawBox(Position, Quaternion.Identity, size, color, true);
        }

        if (Children is null || Children.Length == 0) return;
        
        foreach (var child in Children)
            child.DrawVoxelWireframe();
    }
    
    /// <summary>
    /// Checks whether the given point (in global space) is contained inside the bounds of this
    /// voxel node.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public bool ContainsPoint(Vector3 point)
    {
        var halfSize = Size / 2f;
        return 
            point.X >= Position.X - halfSize && point.X <= Position.X + halfSize &&
            point.Y >= Position.Y - halfSize && point.Y <= Position.Y + halfSize &&
            point.Z >= Position.Z - halfSize && point.Z <= Position.Z + halfSize;
    }
    
}
