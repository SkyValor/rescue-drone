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
    /// If the drone's speed is less than this value, the drone is considered stopped.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10, 0.01")]
    public float StoppingSpeed = 1f;

    /// <summary>
    /// Distance from the target at which the drone starts lowering speed. For example, the distance
    /// before reaching a curve in which the drone begins to decelerate.
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
    /// Distance to register reaching a checkpoint, like for example a waypoint in
    /// a circuit. This value is used during movement, so that the drone reacts to a curve
    /// before reaching it.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 15, 0.01")]
    public float CheckpointRadius = 2f;

    /// <summary>
    /// The percentage of maximum speed the drone can reach when in patrol state.
    /// </summary>
    [Export(PropertyHint.Range, "0.01, 1, 0.01")]
    public float MaxSpeedPercentageAtPatrol = 0.25f;
    
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
    /// How far ahead in time to look when cutting off the player drone's movement while in
    /// close quarters.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 300, 0.1")]
    public float InterceptionPredictionTime = 1.2f;

    /// <summary>
    /// Distance threshold to trigger a direct smash attack against the player drone.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 300, 0.1")]
    public float RammingTriggerDistance = 5f;

    /// <summary>
    /// The drone's max speed when in ramming phase. This value overrides the standard one during the phase.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float RamMaxSpeed = 15f;
    
    /// <summary>
    /// The drone's acceleration when in ramming phase. This value overrides the standard one during the phase.
    /// </summary>
    [Export(PropertyHint.Range, "0, 100, 0.01")] 
    public float RamAcceleration = 5f;

    /// <summary>
    /// How much the enemy drone should recoil after successfully ramming against the player drone.
    /// </summary>
    [Export(PropertyHint.Range, "0, 30, 0.1")]
    public float RamRecoilDistance = 7f;

    public float RamRecoilDuration = 1.5f;
    
    public float RamCooldownDuration = 2.5f;
    
    /// <summary>
    /// Only recalculate the SVO path if the player moves this much.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 50, 0.1")]
    public float RepathThreshold = 2f;

    public float PreferredHeightOffset = 2f;
    
    /// <summary>
    /// Number of directions to look for at each waypoint stop.
    /// </summary>
    [Export(PropertyHint.Range, "1, 10, 1")]
    public int NumberOfScans = 5;
    
    /// <summary>
    /// Seconds to hold a gaze when scanning for the player.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float ScanDuration = 3f;
    
    /// <summary>
    /// The scan settings to be used when the drone is in the state of patrol.
    /// </summary>
    [Export(PropertyHint.ResourceType, "ScanSettings")]
    public ScanSettings PatrolScanSettings { get; private set; }

    /// <summary>
    /// The scan settings to be used when the drone is in the state of search.
    /// </summary>
    [Export(PropertyHint.ResourceType, "ScanSettings")]
    public ScanSettings SearchScanSettings { get; private set; }
    
    /// <summary>
    /// How long the drone holds in idle state before deciding to patrol, in case
    /// where the player drone is not in sight.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 10, 0.1")]
    public float IdleWaitTime = 2f;
}
