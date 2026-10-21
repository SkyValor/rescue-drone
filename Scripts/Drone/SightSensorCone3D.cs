namespace RescueDrone;

using System;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

/// <summary>
/// Performs regular checks to detect drones that are within vision range and distance, by
/// casting rays against inside a cone shape. This class makes use of the addon 'VisionCone3D' which
/// does the heavy lifting.
/// </summary>
[Tool]
[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class SightSensorCone3D : Node3D
{
    public override void _Notification(int what) => this.Notify(what);

    private const string ERROR_MSG = $"'{nameof(SightSensorCone3D)}' requires a single child node with addon script 'VisionCode3D'";
    private const string GET_VISIBLE_BODIES = "get_visible_bodies";

    public event Action<Node3D> BodySighted;
    public event Action<Node3D> BodyHidden;
    
    [Export]
    public float Range
    {
        get => GetCone().Get("range").AsSingle();
        set
        {
            if (IsInsideTree()) GetCone().Set("range", value);
        }
    }

    [Export]
    public float Angle
    {
        get => GetCone().Get("angle").AsSingle();
        set
        {
            if (IsInsideTree()) GetCone().Set("angle", value);
        }
    }
    
    private Node VisionCone { get; set; }

    public void OnReady()
    {
        VisionCone = GetCone();
        if (VisionCone is null) return;

        VisionCone.Connect("body_sighted", Callable.From<Node3D>(OnBodySighted));
        VisionCone.Connect("body_hidden", Callable.From<Node3D>(OnBodyHidden));
    }

    public Node3D[] GetVisibleBodies()
    {
        var visibleBodies = VisionCone.Call(GET_VISIBLE_BODIES).AsGodotArray<Node3D>();
        return visibleBodies.ToArray();
    }

    private Node GetCone()
    {
        if (VisionCone is not null) return VisionCone;
        
        foreach (var child in GetChildren())
        {
            if (!child.HasMethod(GET_VISIBLE_BODIES)) continue;
            
            VisionCone = child;
            break;
        }
        
        return VisionCone ?? throw new Exception(ERROR_MSG);
    }
    
    private void OnBodySighted(Node3D node) => BodySighted?.Invoke(node);
    private void OnBodyHidden(Node3D node) => BodyHidden?.Invoke(node);
    
}
