namespace RescueDrone;

using System;
using Godot;
using Godot.Collections;

[Icon("res://addons/tattomoosa.vision_cone_3d/icons/VisionCone3D.svg")]
public partial class VisionCone3D : Area3D
{
    // public event Action<Node3D> BodySighted;
    // public event Action<Node3D> BodyHidden;
    //
    // public enum VisionTestMode { SampleCenter, SampleRandomVertices }
    //
    // #region Members
    //
    // [Export]
    // public float Range
    // {
    //     get => range;
    //     set
    //     {
    //         range = value;
    //         UpdateShape();
    //     }
    // }
    //
    // [Export]
    // public float Angle
    // {
    //     get => angle;
    //     set
    //     {
    //         angle = value;
    //         UpdateShape();
    //     }
    // }
    //
    // [Export]
    // public bool DebugDraw
    // {
    //     get => debugDraw;
    //     set
    //     {
    //         debugDraw = value;
    //         if (debugDraw && debugVisualizer is null)
    //         {
    //             debugVisualizer = new VisionDebugVisualizer3D();
    //             AddChild(debugVisualizer);
    //         }
    //         else if (!debugDraw && debugVisualizer is not null)
    //         {
    //             debugVisualizer.QueueFree();
    //         }
    //     }
    // }
    //
    // [ExportGroup("Vision Test")]
    // [Export] public VisionTestMode TestMode { get; set; }
    // [Export] public Array<PhysicsBody3D> VisionTestIgnoreBodies { get; set; }
    //
    // [ExportSubgroup("Per-frame probe settings")]
    // [Export] public int VisionTestShapeMaxProbeCount { get; set; } = 5;
    // [Export] public int VisionTestMaxBodyCount { get; set; } = 10;
    //
    // // [ExportGroup("Collision")]
    // // [Export(PropertyHint.Layers3DPhysics)]
    // // public int CollisionLayer { get; set; }
    //
    // private float range;
    // private float angle;
    // private bool debugDraw;
    //
    // private VisionConeDebugVisualizer3D debugVisualizer;
    //
    // #endregion
    //
    // private void UpdateShape()
    // {
    //     
    // }
    
}
