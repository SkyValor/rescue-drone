namespace RescueDrone;

public partial class EnemyLogic
{
    public static class Input
    {
        public readonly record struct PhysicsTick(float DeltaTime);
        public readonly record struct FinishedLookout;
    }
}
