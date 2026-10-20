namespace RescueDrone;

using System.Collections.Generic;
using Godot;

[Tool]
public partial class ConeShape3D : ConvexPolygonShape3D
{
    /// <summary>
    /// The height of the cone.
    /// </summary>
    [Export]
    public float Height
    {
        get => height;
        set
        {
            height = value;
            RequestResize();
        }
    }

    /// <summary>
    /// The radius of the bottom of the cone.
    /// </summary>
    [Export]
    public float Radius
    {
        get => radius;
        set
        {
            radius = value;
            RequestResize();
        }
    }

    /// <summary>
    /// The number of radial segments of the cone.
    /// </summary>
    [Export]
    public int Resolution
    {
        get => resolution;
        set
        {
            resolution = value;
            RequestResize();
        }
    }

    private float height;
    private float radius;
    private int resolution;
    private bool pendingResize;

    private void RequestResize()
    {
        // Will only resize once per frame, during idle time
        if (pendingResize) return;

        CallDeferred(MethodName.UpdateSize);
        pendingResize = true;
    }

    private void UpdateSize()
    {
        Points = MakeConePolygonPoints();
        pendingResize = false;
    }

    private Vector3[] MakeConePolygonPoints()
    {
        var points = new List<Vector3>();
        var top = new Vector3(0, Height / 2f, 0);
        for (int i = 0; i < Resolution; i++)
        {
            var angle = i * Mathf.Tau / Resolution;
            var x = Mathf.Cos(angle) * Radius;
            var y = Mathf.Sin(angle) * Radius;
            points.Add(new Vector3(x, -Height / 2f, y));
        }
        points.Add(top);
        return points.ToArray();
    }
    
}
