namespace RescueDrone;

public partial class EnemyLogic
{
    public record Settings(
        float VisionRange,

        float PlayerMinDistance,

        float LookoutAngle,
        float LookoutRotationTime,
        float LookoutHoldDuration);
}
