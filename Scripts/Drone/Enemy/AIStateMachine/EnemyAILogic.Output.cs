namespace RescueDrone;

using Godot;

public partial class EnemyAILogic
{
    public static class Output
    {
        public readonly record struct MoveTowards(Vector3 TargetPosition, float Delta);
        public readonly record struct RotationComputed(Transform3D GlobalTransform);
        public readonly record struct VelocityComputed(Vector3 Velocity);
        public readonly record struct SetCurrentMaxSpeedPercentage(float MaxSpeedPercentage);
    }
}
