namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class HoverBobSettings : Resource
{
    /// <summary>
    /// Frequency for the sine function of the bobbing effect. This will increase the number of times the effect
    /// completes one full cycle in a given period.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 20, 0.01")]
    public float Frequency { get; private set; } = 2f;

    /// <summary>
    /// Amplitude for the sine function of the bobbing effect. This will stretch the maximum and minimum values
    /// that the end result will reach.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 1, 0.01")]
    public float Amplitude { get; private set; } = 0.05f;
}
