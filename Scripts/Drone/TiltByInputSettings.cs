namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class TiltByInputSettings : Resource
{
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
