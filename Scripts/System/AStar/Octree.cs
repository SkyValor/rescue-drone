namespace RescueDrone;

using System.Collections.Generic;
using Godot;

public class Octree
{
    public OctreeNode Root;
    public Aabb Bounds;    // Help us keep track of what's inside the tree and inside every node.
    public readonly Graph Graph;

    private readonly List<OctreeNode> emptyLeaves = [];

    public Octree(Node3D[] worldObjects, float minNodeSize, Graph graph)
    {
        Graph = graph;
        
        CalculateBounds(worldObjects);
        CreateTree(worldObjects, minNodeSize);

        GetEmptyLeaves(Root);
        GetEdges();
        GD.Print(Graph.Edges.Count);
    }

    private void CreateTree(Node3D[] worldObjects, float minNodeSize)
    {
        Root = new OctreeNode(Bounds, minNodeSize);
        foreach (var obj in worldObjects)
        {
            Root.Divide(obj);
        }
    }

    private void GetEmptyLeaves(OctreeNode node)
    {
        if (node.IsLeaf && node.Objects.Count == 0)
        {
            emptyLeaves.Add(node);
            Graph.AddNode(node);
            return;
        }

        if (node.Children is null) return;

        foreach (var child in node.Children)
        {
            GetEmptyLeaves(child);
        }

        for (int i = 0; i < node.Children.Length; i++)
        {
            for (int j = i + 1; j < node.Children.Length; j++)
            {
                Graph.AddEdge(node.Children[i], node.Children[j]);
            }
        }
    }

    private void GetEdges()
    {
        foreach (var leaf in emptyLeaves)
        {
            foreach (var otherLeaf in emptyLeaves)
            {
                if (leaf.Bounds.Intersects(otherLeaf.Bounds))
                    Graph.AddEdge(leaf, otherLeaf);
            }
        }
    }

    // Calculate the overall bounds that encapsulate every world object.
    private void CalculateBounds(Node3D[] worldObjects)
    {
        foreach (var obj in worldObjects)
        {
            var collision = Utils.GetChildNode<CollisionShape3D>(obj);
            if (collision is null) continue;
            
            var colliderVertices = GetColliderVertices(collision);
            if (colliderVertices is null) continue;
            
            foreach (var colliderPoint in colliderVertices)
                Bounds = Bounds.Expand(colliderPoint);
        }
        
        SetBoundsMinMax();
    }

    private static Vector3[] GetColliderVertices(CollisionShape3D collider)
    {
        // We are only working with BoxShape3D CollisionShapes.
        if (collider.Shape is not BoxShape3D shape) 
            return null;
        
        return
        [
            collider.GlobalPosition + Vector3.Right * shape.Size.X * 0.5f,
            collider.GlobalPosition - Vector3.Right * shape.Size.X * 0.5f,
            collider.GlobalPosition + Vector3.Up * shape.Size.Y * 0.5f,
            collider.GlobalPosition - Vector3.Up * shape.Size.Y * 0.5f,
            collider.GlobalPosition + Vector3.Forward * shape.Size.Z * 0.5f,
            collider.GlobalPosition - Vector3.Forward * shape.Size.Z * 0.5f
        ];
    }

    private void SetBoundsMinMax()
    {
        var size = Vector3.One * GetMax(Bounds.Size.X, Bounds.Size.Y, Bounds.Size.Z) * 0.6f;
        Bounds = Bounds.Expand(Bounds.GetCenter() - size);
        Bounds = Bounds.Expand(Bounds.GetCenter() + size);
    }

    private static float GetMax(float x, float y, float z)
    {
        if (x > y && x > z) return x;
        return y > z ? y : z;
    }
    
}
