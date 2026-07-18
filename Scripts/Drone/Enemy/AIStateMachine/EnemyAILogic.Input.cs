namespace RescueDrone;

public partial class EnemyAILogic
{
    public static class Input
    {
        public readonly record struct ReturnToIdle;
        
        public readonly record struct Enable;
        public readonly record struct PhysicsTick(double Delta);
        public readonly record struct Moved;

        public readonly record struct MoveToCircuit;
        public readonly record struct MoveToWaypoint;
        public readonly record struct StartScanning;
        
        public readonly record struct InitiateRotatingLeft;
        public readonly record struct InitiateRotatingRight;
        public readonly record struct FinishedLookout;
    }
}
