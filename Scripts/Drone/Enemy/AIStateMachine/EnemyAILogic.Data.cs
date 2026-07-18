namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    [Meta, Id("enemy_ai_logic_data")]
    public partial record Data
    {
        public float CurrentTargetSpeed { get; set; }
        public Vector3[] SVOPath { get; set; }
        public int CurrentPathIndex { get; set; }
        
        public WaypointCircuit CurrentCircuit { get; set; }
        public Waypoint CurrentWaypoint { get; set; }
    }
}
