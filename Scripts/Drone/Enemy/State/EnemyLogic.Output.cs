namespace RescueDrone;

using Godot;

public partial class EnemyLogic
{
    public static class Output
    {
        public readonly record struct VelocityChanged(Vector3 Velocity);
    }
}
