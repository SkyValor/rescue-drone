namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class ScanSettings : Resource
{
    /// <summary>
    /// Number of directions to look for at each waypoint stop.
    /// </summary>
    [Export(PropertyHint.Range, "1, 10, 1")]
    public int NumberOfScans = 5;
        
    /// <summary>
    /// Seconds to hold a gaze when scanning for the player.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float Duration = 3f;

    /// <summary>
    /// The total angle that the drone can rotate around its Y-axis to compute a new scanning direction.
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 180.0, 0.1")]
    public float AngleInX = 45f;

    /// <summary>
    /// The total angle that the drone can rotate around its X-axis to compute a new scanning direction.
    /// </summary>
    [Export(PropertyHint.Range, "1.0, 90.0, 0.1")]
    public float AngleInY = 15f;
}
