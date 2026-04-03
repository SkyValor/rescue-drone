namespace RescueDrone;

using Godot;

public partial class EnemyLogic
{
    public static class Output
    {
        public readonly record struct VelocityChanged(Vector3 Velocity);
        public readonly record struct MoveTowards(Vector3 TargetPosition, float delta);
        public readonly record struct MovementRequest(Vector3 Direction, float Delta);
        public readonly record struct RotationRequest(Vector3 TargetRotation, float Delta);
    }
}
