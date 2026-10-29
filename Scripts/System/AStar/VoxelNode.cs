namespace RescueDrone;

using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public class VoxelNode
{
    public static event System.Action<VoxelNode, VoxelNode[]> OnNodeSubdivided;
    public static event System.Action<VoxelNode, VoxelNode[]> OnNodePartiallySubdivided; 
    
    public readonly int Id;
    private static int nextId;

    private static readonly Color RootWhite = new(Colors.White, 0.5f);
    private static readonly Color EmptyLeafGreen = new(Colors.Green, 0.4f);
    private static readonly Color OccupiedLeafRed = new(Colors.Red, 0.25f);
    
    public VoxelNode Parent { get; }
    public Vector3 Position { get; }
    public float Size { get; }

    public bool IsEmpty { get; private init; }
    public VoxelNode[] Children { get; private set; } = [];
    public bool IsLeaf => Children.Length == 0;

    public VoxelNode(Vector3 position, float size, VoxelNode parent)
    {
        Id = nextId++;
        
        Parent = parent;
        Position = position;
        Size = size;
    }
    
    /// <summary>
    /// Draw a wireframe box of this node and recursively call its children to do the same. Only the root
    /// node and leaf nodes are drawn.
    /// </summary>
    public void DrawVoxelWireframe()
    {
        var size = new Vector3(Size, Size, Size);
        
        if (Parent is null) DebugDraw3D.DrawBox(Position, Quaternion.Identity, size, RootWhite, true);
        if (IsLeaf || Children.Any(child => child is null))
        {
            var color = IsEmpty ? EmptyLeafGreen : OccupiedLeafRed;
            DebugDraw3D.DrawBox(Position, Quaternion.Identity, size, color, true);
        }

        if (Children is null || Children.Length == 0) return;

        foreach (var child in Children)
            child?.DrawVoxelWireframe();
    }
    
    /// <summary>
    /// Checks whether the given point (in global space) is contained inside the bounds of this
    /// voxel node.
    /// </summary>
    public bool ContainsPoint(Vector3 point)
    {
        var halfSize = Size / 2f;
        return 
            point.X >= Position.X - halfSize && point.X <= Position.X + halfSize &&
            point.Y >= Position.Y - halfSize && point.Y <= Position.Y + halfSize &&
            point.Z >= Position.Z - halfSize && point.Z <= Position.Z + halfSize;
    }

    /// <summary>
    /// Continuously subdivide this node until there is no collision,
    /// or it hits the minimum node size limit, or it hits the subdivision limit.
    /// </summary>
    public void Subdivide(World3D world, float minNodeSize, int remainingSubdivisions)
    {
        // Stop subdividing if we hit our subdivision limit or minimum node size limit
        if (remainingSubdivisions <= 0 || Size * 0.5f < minNodeSize) return;

        var halfSize = Size * 0.5f;
        var quarterSize = halfSize * 0.5f;
        var offsets = GetOffsetPositionArray(quarterSize);

        Children = new VoxelNode[8];
        var hasChildren = false;

        for (int i = 0; i < 8; i++)
        {
            var childPosition = Position + offsets[i];
            if (CheckCollision(world, childPosition, halfSize))
            {
                var child = new VoxelNode(childPosition, halfSize, this) { IsEmpty = false };
                Children[i] = child;
                hasChildren = true;
            
                // Recursively subdivide the child
                child.Subdivide(world, minNodeSize, remainingSubdivisions - 1);
            }
        }

        // If no children intersected geometry, the children array is empty (acts as a leaf)
        if (hasChildren)
            OnNodeSubdivided?.Invoke(this, Children);
        else
            Children = [];
    }

    /// <summary>
    /// Continuously subdivide this node until it hits the minimum node size limit.
    /// </summary>
    public void SubdivideInDepth(World3D world, float minNodeSize)
    {
        // Stop subdividing if we hit our minimum node size limit
        if (Size * 0.5f < minNodeSize) return;
        
        var halfSize = Size * 0.5f;
        var quarterSize = halfSize * 0.5f;
        var offsets = GetOffsetPositionArray(quarterSize);
        
        Children = new VoxelNode[8];
        for (int i = 0; i < 8; i++)
        {
            var childPosition = Position + offsets[i];
            var child = CheckCollision(world, childPosition, halfSize) 
                ? new VoxelNode(childPosition, halfSize, this) { IsEmpty = false } 
                : new VoxelNode(childPosition, halfSize, this) { IsEmpty = true };
            Children[i] = child;
                
            // Recursively subdivide the child
            child.SubdivideInDepth(world, minNodeSize);
        }
        
        OnNodeSubdivided?.Invoke(this, Children);
    }
    
    /// <summary>
    /// Recursively collects all leaf nodes that are empty (not occupied by collision).
    /// </summary>
    public void GetEmptyLeafNodes(List<VoxelNode> results)
    {
        if (IsLeaf)
        {
            if (IsEmpty) results.Add(this);
        }
        else
        {
            foreach (var child in Children)
                child?.GetEmptyLeafNodes(results);
        }
    }
    
    /// <summary>
    /// Finds the deepest existing node at a position without modifying the tree.
    /// </summary>
    public VoxelNode FindDeepestExistingNode(Vector3 targetPosition)
    {
        // Check bounds
        if (!ContainsPoint(targetPosition)) return null;

        // If this is a leaf or has no children initialized, this is the deepest available node
        if (IsLeaf) return this;

        var index = GetOctantIndex(targetPosition);
        return Children[index] == null ? this : Children[index].FindDeepestExistingNode(targetPosition);
    }
    
    /// <summary>
    /// Finds or dynamically creates/subdivides down to minimum node size at a specific world position.
    /// </summary>
    public VoxelNode FindOrCreateAtPosition(Vector3 targetPosition, float minNodeSize, World3D world)
    {
        var halfSize = Size * 0.5f;
        
        // Ensure target is within bounds
        if (!ContainsPoint(targetPosition)) return null;

        // If we've reached min node size, return this node
        if (Size <= minNodeSize || halfSize < minNodeSize)
            return this;

        if (Children.Length == 0)
            Children = new VoxelNode[8];

        var index = GetOctantIndex(targetPosition);
        if (Children[index] == null)
        {
            var quarterSize = halfSize * 0.5f;
            var offsets = GetOffsetPositionArray(quarterSize);

            var childPosition = Position + offsets[index];
            var occupied = CheckCollision(world, childPosition, halfSize);
            Children[index] = new VoxelNode(childPosition, halfSize, this) { IsEmpty = !occupied };
            OnNodePartiallySubdivided?.Invoke(this, Children);
        }

        return Children[index].FindOrCreateAtPosition(targetPosition, minNodeSize, world);
    }
    
    /// <summary>
    /// Draw a box shape to detect collision against the world environment.
    /// </summary>
    private static bool CheckCollision(World3D world, Vector3 center, float size)
    {
        if (world?.DirectSpaceState == null) 
            return false;

        var spaceState = world.DirectSpaceState;
        
        var query = new PhysicsShapeQueryParameters3D();
        var boxShape = new BoxShape3D();
        boxShape.Size = new Vector3(size, size, size);

        query.Shape = boxShape;
        query.Transform = new Transform3D(Basis.Identity, center);
        query.CollisionMask = Get3DLayerValueByName("WorldGeometry") | Get3DLayerValueByName("WorldBounds");

        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }

    private static uint Get3DLayerValueByName(string layerName)
    {
        for (int i = 1; i < 33; i++)
        {
            var settingName = $"layer_names/3d_physics/layer_{i}";
            var configuredName = ProjectSettings.GetSetting(settingName, "");
            if (configuredName.VariantType is Variant.Type.String && configuredName.AsString().Equals(layerName))
                return (uint) 1 << (i - 1); // Returns the bitmask value (e.g., 1, 2, 4, 8...)
        }

        return 0;
    }

    private static int Get3DLayerIndexByName(string layerName)
    {
        for (int i = 1; i < 33; i++)
        {
            var settingName = $"layer_names/3d_physics/layer_{i}";
            var configuredName = ProjectSettings.GetSetting(settingName, "");
            if (configuredName.VariantType is Variant.Type.String && configuredName.AsString().Equals(layerName))
                return i;
        }

        return -1;
    }
    
    /// <summary>
    /// Determine octant index using bitwise flags (X, Y, Z).
    /// </summary>
    private int GetOctantIndex(Vector3 targetPosition)
    {
        var index = 0;
        if (targetPosition.X >= Position.X) index |= 1;
        if (targetPosition.Y >= Position.Y) index |= 2;
        if (targetPosition.Z >= Position.Z) index |= 4;
        return index;
    }

    /// <summary>
    /// Returns an array of positions relative to a center point that represent the
    /// eight octants of a voxel node.
    /// </summary>
    private static Vector3[] GetOffsetPositionArray(float quarterSize)
    {
        return
        [
            new Vector3(-quarterSize, -quarterSize, -quarterSize), // 000
            new Vector3( quarterSize, -quarterSize, -quarterSize), // 001
            new Vector3(-quarterSize,  quarterSize, -quarterSize), // 010
            new Vector3( quarterSize,  quarterSize, -quarterSize), // 011
            new Vector3(-quarterSize, -quarterSize,  quarterSize), // 100
            new Vector3( quarterSize, -quarterSize,  quarterSize), // 101
            new Vector3(-quarterSize,  quarterSize,  quarterSize), // 110
            new Vector3( quarterSize,  quarterSize,  quarterSize)  // 111
        ];
    }

}
