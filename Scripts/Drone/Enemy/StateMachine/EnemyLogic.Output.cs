namespace RescueDrone;

using Godot;

public partial class EnemyLogic
{
    public static class Output
    {
        public readonly record struct MoveTowards(Vector3 TargetPosition, float Delta);
    }
}
