namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class EnemyDroneSettings : Resource
{
    /// <summary>
    /// The drone's maximum speed when traveling.
    /// </summary>
    [ExportCategory("Speed Settings")] 
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float MaxSpeed = 15f;

    /// <summary>
    /// The velocity weight when speed interpolates to the maximum. 
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 100, 0.01")] 
    public float Acceleration = 5f;

    /// <summary>
    /// The velocity weight when speed interpolates to zero. Higher value means breaking harder before turns
    /// or when halting.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 100, 0.01")]
    public float Deceleration = 8f;
    
    /// <summary>
    /// The weight when rotating the drone around the Y-axis.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 100, 0.01")]
    public float TurnSpeed = 20;

    /// <summary>
    /// Distance from the target at which the drone starts lowering speed.
    /// </summary>
    [ExportCategory("Momentum Settings")] 
    [Export(PropertyHint.Range, "0.1, 20, 0.1")]
    public float BreakingDistance = 6f;
    
    /// <summary>
    /// The minimum percentage of max speed the drone should drop to on a 180º turn.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 1, 0.01")]
    public float MinTurnSpeedPercentage = 0.25f;
    
    /// <summary>
    /// Distance to register reaching a checkpoint.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 15, 0.01")]
    public float CheckpointRadius = 2f;
    
    /// <summary>
    /// Minimum distance to the player that is permitted before the drone starts drifting away.
    /// </summary>
    [ExportCategory("Player Seeking Settings")]
    [Export(PropertyHint.Range, "0.1, 100, 0.1")]
    public float MinDistance = 4f;
    
    /// <summary>
    /// Maximum distance to the player that is permitted before the drone starts closing in.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 300, 0.1")]
    public float MaxDistance = 7f;
    
    /// <summary>
    /// Only recalculate the SVO path if the player moves this much.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 50, 0.1")]
    public float RepathThreshold = 2f;
    
    /// <summary>
    /// Number of directions to look for at each waypoint stop.
    /// </summary>
    [Export(PropertyHint.Range, "1, 10, 1")]
    public int NumberOfScans = 5;
    
    /// <summary>
    /// Seconds to hold a gaze when scanning for the player.
    /// </summary>
    [Export(PropertyHint.Range, "1, 10, 0.1")]
    public float ScanWaitTime = 3f;
}
