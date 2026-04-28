namespace RescueDrone;

public partial class EnemyLogic
{
    public record Settings(
        float VisionRange,

        float PlayerMinDistance,

        float PatrolLookoutAngle,
        float PatrolLookoutRotationTime,
        float PatrolLookoutHoldDuration,
        
        float SearchLookoutAngle,
        float SearchLookoutRotationTime,
        float SearchLookoutHoldDuration);
}
