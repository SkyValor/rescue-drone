namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    [Meta, Id("enemy_ai_logic_data")]
    public partial record Data
    {
        public float CurrentTargetSpeed { get; set; }
        
        /// <summary>
        /// The point pathway that the enemy drone is using in order to travel in 3D space.
        /// </summary>
        public Vector3[] SVOPath { get; set; }
        
        /// <summary>
        /// The current index to get the corresponding target point from the SVO pathway.
        /// </summary>
        public int CurrentPathIndex { get; set; }
        
        /// <summary>
        /// Current circuit that the enemy drone is circulating in order to scan around.
        /// </summary>
        public WaypointCircuit CurrentCircuit { get; set; }
        
        /// <summary>
        /// Target waypoint that the enemy drone is traveling to or currently at.
        /// </summary>
        public Waypoint CurrentWaypoint { get; set; }
        
        /// <summary>
        /// The most recent player position (in global space) that was detected by the sight sensor.
        /// </summary>
        public Vector3 LastPlayerPosition { get; set; }
        
        /// <summary>
        /// The most recent player position (in global space) that was registered when the drone
        /// has recalculated its SVO pathway.
        /// </summary>
        public Vector3 LastRepathPosition { get; set; }
        
        /// <summary>
        /// Whether the player drone has been detected in this frame.
        /// The node process order is top to bottom, so likely the player position has not been updated this frame.
        /// </summary>
        public bool PlayerDetectedThisFrame { get; set; }
    }
}
