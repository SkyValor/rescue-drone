namespace RescueDrone;

using Godot;

public class OctreeObject
{
    private Aabb bounds;

    public OctreeObject(Node3D obj)
    {
        var collision = Utils.GetChildNode<CollisionShape3D>(obj);
        if (collision?.Shape is not BoxShape3D shape)
            return;

        // The position is the bottom-left and forward point.
        var position = new Vector3
        {
            X = collision.GlobalPosition.X - shape.Size.X * 0.5f,
            Y = collision.GlobalPosition.Y - shape.Size.Y * 0.5f,
            Z = collision.GlobalPosition.Z - shape.Size.Z * 0.5f
        };
        
        bounds = new Aabb(position, shape.Size);
    }

    public bool Intersects(Aabb boundsToCheck) => bounds.Intersects(boundsToCheck);

}
