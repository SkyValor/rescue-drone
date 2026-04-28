namespace RescueDrone;

public partial class EnemyLogic
{
    public static class Input
    {
        public readonly record struct PhysicsTick(float DeltaTime);
        
        public readonly record struct MoveToWaypoint;
        public readonly record struct InitiateRotatingLeft;
        public readonly record struct InitiateRotatingRight;
        public readonly record struct FinishedLookout;
    }
}
