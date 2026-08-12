namespace RescueDrone;

using Godot;

public partial class EnemyAILogic
{
    public record Settings(
        // Drone information
        float DroneRadius,
        Rid DroneRID,
        // Speed settings
        float MaxSpeed, 
        float Acceleration, 
        float Deceleration, 
        float TurnSpeed, 
        // Momentum settings
        float BreakingDistance,
        float MinTurnSpeedPercentage,
        // Scan
        int NumberOfScans,
        float ScanWaitTime,
        // Player seeking settings
        float MinDistance,
        float MaxDistance,
        float RepathThreshold);
}
