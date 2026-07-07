namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public class OctreeNodeShape
{
    public readonly int Id;
    private static int nextId;

    public readonly Vector3 Position;
    public OctreeNodeShape[] Children;
    public Vector3 Size => boxShape.Size;
    public bool IsLeaf => Children is null || Children.Length == 0;
    public bool IsEmpty { get; private set; } = true;

    private readonly OctreeNodeShape Parent;
    private readonly BoxShape3D boxShape;
    private readonly float minNodeSize;
    private readonly World3D world3D;

    // The public constructor does not accept a parent as it will be the root node.
    public OctreeNodeShape(Vector3 position, Vector3 size, float minNodeSize, World3D world3D) : this(position, size, null, minNodeSize, world3D) { }

    // The private constructor accepts a parent as it will be created from a subdivision.
    private OctreeNodeShape(Vector3 position, Vector3 size, OctreeNodeShape parent, float minNodeSize, World3D world3D)
    {
        Id = nextId++;
        Parent = parent;
        Position = position;
        
        this.minNodeSize = minNodeSize;
        this.world3D = world3D;

        boxShape = new BoxShape3D();
        boxShape.Size = size;
    }

    public void DrawNode()
    {
        if (Parent is null) 
            DebugDraw3D.DrawBox(Position, Quaternion.Identity, Size, Colors.Green, true);
        
        // if (IsLeaf && IsEmpty)
            // DebugDraw3D.DrawBox(Position, Quaternion.Identity, Size, new Color(0.827f, 0.827f, 0.827f), true);
        
        if (IsLeaf && !IsEmpty) 
            DebugDraw3D.DrawBox(Position, Quaternion.Identity, Size, Colors.Red, true);
        
        if (Children is null) return;
        
        foreach (var child in Children)
            child?.DrawNode();
    }

    // A sphere with this radius can go through if the radius is less than half the size.
    public bool CanSphereGoThrough(float sphereRadius)
    {
        return sphereRadius < Size.X * 0.5f;
    }
    
    public bool ContainsPoint(Vector3 point)
    {
        var halfSize = boxShape.Size * 0.5f;
        return 
            point.X >= Position.X - halfSize.X && point.X <= Position.X + halfSize.X &&
            point.Y >= Position.Y - halfSize.Y && point.Y <= Position.Y + halfSize.Y &&
            point.Z >= Position.Z - halfSize.Z && point.Z <= Position.Z + halfSize.Z;
    }

    public void Subdivide(ref List<OctreeNodeShape> emptyLeaves)
    {
        if (CheckVoxelCollision(world3D))
        {
            IsEmpty = false;
            
            // If the subdivision results in voxels too small, interrupt it.
            var halfSize = boxShape.Size * 0.5f;
            if (halfSize.X < minNodeSize) return;
            
            // Subdivide this node into 8 nodes of half size.
            Children ??= new OctreeNodeShape[8];
            var centerOffset = boxShape.Size * 0.25f;
            for (int i = 0; i < 8; i++)
            {
                var childPosition = Position;
                childPosition.X += centerOffset.X * ((i & 1) == 0 ? -1 : 1);
                childPosition.Y += centerOffset.Y * ((i & 2) == 0 ? -1 : 1);
                childPosition.Z += centerOffset.Z * ((i & 4) == 4 ? -1 : 1);
            
                Children[i] ??= new OctreeNodeShape(childPosition, halfSize, this, minNodeSize, world3D);
                Children[i].Subdivide(ref emptyLeaves);
            }
        }
        
        if (IsLeaf && IsEmpty) 
            emptyLeaves.Add(this);
    }

    // Continue to subdivide until reaching minimum node size.
    public void SubdivideDepth(ref List<OctreeNodeShape> emptyLeaves)
    {
        // If this node is already at minimum size, check for collision.
        var halfSize = boxShape.Size * 0.5f;
        if (halfSize.X < minNodeSize)
        {
            if (CheckVoxelCollision(world3D))
                IsEmpty = false;
            else
                emptyLeaves.Add(this);

            return;
        }
        
        // Otherwise, subdivide this node into 8 nodes of half size.
        Children ??= new OctreeNodeShape[8];
        var centerOffset = boxShape.Size * 0.25f;
        for (int i = 0; i < 8; i++)
        {
            var childPosition = Position;
            childPosition.X += centerOffset.X * ((i & 1) == 0 ? -1 : 1);
            childPosition.Y += centerOffset.Y * ((i & 2) == 0 ? -1 : 1);
            childPosition.Z += centerOffset.Z * ((i & 4) == 4 ? -1 : 1);
        
            Children[i] ??= new OctreeNodeShape(childPosition, halfSize, this, minNodeSize, world3D);
            Children[i].SubdivideDepth(ref emptyLeaves);
        }
    }

    private bool CheckVoxelCollision(World3D world3d)
    {
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = boxShape,
            Transform = new Transform3D(Basis.Identity, Position),
            CollisionMask = (1 << 4 - 1) | (1 << 8 - 1) // Buildings are set in layer 4. Invisible boundaries in 8.
        };
        var spaceState = world3d.DirectSpaceState;
        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }
    
}
