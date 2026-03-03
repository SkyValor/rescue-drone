namespace RescueDrone;

public partial class EnemyLogic
{
    public static class Input
    {
        public readonly record struct PhysicsTick(double DeltaTime);
    }
}
