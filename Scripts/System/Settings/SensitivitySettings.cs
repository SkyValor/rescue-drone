namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class SensitivitySettings : Resource
{
    private const float DIVISOR = 0.0001f;

    [Export(PropertyHint.Range, "1, 10, 1")] 
    public int Index { get; set; }
    
    [Export] private int MinSensitivity { get; set; }
    [Export] private int MaxSensitivity { get; set; }
    [Export] private int SensitivityStep { get; set; }
    
    /// <summary>
    /// Gets the multiplier value to be used against the user input. 
    /// </summary>
    /// <returns></returns>
    public float GetSensitivityMultiplier() => Index * SensitivityStep * DIVISOR;
}
