namespace RescueDrone;

using Godot;

public partial class EnemyLogic
{
    public sealed class Data
    {
        public Vector3 LastPlayerKnownPosition;

        public float LookoutDuration;
        public float LookoutTimeRemaining;
    }
}

