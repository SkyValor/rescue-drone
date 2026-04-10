namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public class OctreeNode
{
    public readonly int Id;
    private static int nextId;
    
    public readonly List<OctreeObject> Objects = [];

    public Aabb Bounds;
    public OctreeNode[] Children;
    public bool IsLeaf => Children == null || Children.Length == 0;
    
    private readonly Aabb[] childBounds = new Aabb[8];
    private readonly float minNodeSize;

    public OctreeNode(Aabb bounds, float minNodeSize)
    {
        Id = nextId++;
        
        Bounds = bounds;
        this.minNodeSize = minNodeSize;

        var newSize = bounds.Size * 0.5f;
        var centerOffset = bounds.Size * 0.25f;
        var parentCenter = bounds.GetCenter();

        for (int i = 0; i < 8; i++)
        {
            var childCenter = parentCenter;
            childCenter.X += centerOffset.X * ((i & 1) == 0 ? -1 : 1);
            childCenter.Y += centerOffset.Y * ((i & 2) == 0 ? -1 : 1);
            childCenter.Z += centerOffset.Z * ((i & 4) == 0 ? -1 : 1);

            // When creating the AABB, the first argument is the MinPosition, not the center.
            var origin = childCenter - centerOffset;
            childBounds[i] = new Aabb(origin, newSize);
        }
    }

    public void Divide(Node3D obj) => Divide(new OctreeObject(obj));

    private void Divide(OctreeObject obj)
    {
        if (Bounds.Size.X <= minNodeSize)
        {
            AddObject(obj);
            return;
        }
        
        Children ??= new OctreeNode[8];
        bool intersectedChild = false;
        for (int i = 0; i < 8; i++)
        {
            Children[i] ??= new OctreeNode(childBounds[i], minNodeSize);
            if (obj.Intersects(childBounds[i]))
            {
                Children[i].Divide(obj);
                intersectedChild = true;
            }
        }

        if (!intersectedChild)
        {
            AddObject(obj);
        }
    }
    
    private void AddObject(OctreeObject obj) => Objects.Add(obj);

    public void DrawNode()
    {
        DebugDraw3D.DrawBox(Bounds.GetCenter(), Quaternion.Identity, Bounds.Size, Colors.Green, true);

        foreach (var obj in Objects)
        {
            if (obj.Intersects(Bounds))
                DebugDraw3D.DrawBox(Bounds.GetCenter(), Quaternion.Identity, Bounds.Size, Colors.Red, true);
        }
        
        if (Children is null || Children.Length == 0) return;
        foreach (var child in Children)
            child?.DrawNode();
    }
    
}
