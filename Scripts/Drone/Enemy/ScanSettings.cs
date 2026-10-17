namespace RescueDrone;

using Godot;

// TODO: Have Duration be a range instead

[GlobalClass]
public partial class ScanSettings : Resource
{
    /// <summary>
    /// Minimum number of scans the drone will perform during scanning phase.
    /// </summary>
    [Export(PropertyHint.Range, "1, 20, 1")]
    public int MinNumberOfScans = 1;
    
    /// <summary>
    /// Maximum number of scans the drone will perform during scanning phase.
    /// </summary>
    [Export(PropertyHint.Range, "1, 20, 1")]
    public int MaxNumberOfScans = 10;
        
    /// <summary>
    /// Seconds to hold a gaze when scanning for the player.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float Duration = 3f;

    /// <summary>
    /// Minimum duration time for the drone to hold during scanning phase.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float MinDuration = 1f;
    
    /// <summary>
    /// Maximum duration time for the drone to hold during scanning phase.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float MaxDuration = 4f;

    /// <summary>
    /// The total angle that the drone can rotate around its Y-axis to compute a new scanning direction.
    /// A value of 90 will have the drone have a possible field of vision of 90 degrees (45 to the left at most).
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 180.0, 0.1")]
    public float AngleInX = 45f;

    /// <summary>
    /// The total angle that the drone can rotate around its X-axis to compute a new scanning direction.
    /// A value of 90 will have the drone have a possible field of vision of 90 degrees (45 upwards at most).
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 90.0, 0.1")]
    public float AngleInY = 15f;

    /// <summary>
    /// The weight when rotating the drone to align its nose with the target scan direction.
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float RotationSpeed = 10f;
}
