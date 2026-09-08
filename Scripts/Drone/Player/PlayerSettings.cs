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
    /// The velocity weight when vertical speed interpolates to the maximum.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")]
    public float VerticalAcceleration { get; private set; } = 15f;

    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.Range, "0, 5, 0.01")]
    public float StoppingSpeed { get; private set; } = 0.2f;
    
    /// <summary>
    /// The weight when rotating the drone around the Y-axis.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float RotationSpeed { get; private set; } = 12f;
    
    /// <summary>
    /// Frequency for the sine function of the bobbing effect. This will increase the number of times the effect
    /// completes one full cycle in a given period.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 20, 0.01")] 
    public float HoverBobFrequency { get; private set; } = 2f;
    
    /// <summary>
    /// Amplitude for the sine function of the bobbing effect. This will stretch the maximum and minimum values
    /// that the end result will reach.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 1, 0.01")] 
    public float HoverBobAmplitude { get; private set; } = 0.05f;
}
