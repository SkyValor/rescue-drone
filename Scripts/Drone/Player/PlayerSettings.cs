namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class PlayerSettings : Resource
{
    /// <summary>
    /// The drone's maximum speed when traveling horizontally.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float MaxSpeed { get; private set; } = 5f;

    /// <summary>
    /// The drone's maximum speed when ascending or descending.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")]
    public float MaxVerticalSpeed { get; private set; } = 5f;
    
    /// <summary>
    /// The velocity weight when horizontal speed interpolates to the maximum.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float Acceleration { get; private set; } = 25f;
   
    /// <summary>
    /// The velocity weight when horizontal speed interpolates to zero.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")]
    public float Deceleration { get; private set; } = 20f;

    /// <summary>
    /// The velocity weight when vertical speed interpolates to the maximum.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")]
    public float VerticalAcceleration { get; private set; } = 15f;

    /// <summary>
    /// The velocity weight when vertical speed interpolates to zero.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")]
    public float VerticalDeceleration { get; private set; } = 10f;

    /// <summary>
    /// If the drone's speed is less than this value, the drone is considered stopped.
    /// </summary>
    [Export(PropertyHint.Range, "0, 5, 0.01")]
    public float StoppingSpeed { get; private set; } = 0.2f;
    
    /// <summary>
    /// The weight when rotating the drone around the Y-axis.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float RotationSpeed { get; private set; } = 12f;

    /// <summary>
    /// The maximum value in degrees that the drone can rotate in any direction
    /// when tilting during movement.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 90, 0.01")]
    public float MaxTiltAngleDegrees { get; private set; } = 25f;
    
    /// <summary>
    /// The weight when rotating the drone for the tilting feature.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 20, 0.01")]
    public float TiltLerpSpeed { get; private set; } = 6f;
}
