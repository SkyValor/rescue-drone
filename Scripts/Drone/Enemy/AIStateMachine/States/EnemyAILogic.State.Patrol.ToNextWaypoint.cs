namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record ToNextWaypoint : Patrol
        {
            public Waypoint NextWaypoint { get; set; }
        }
    }
}
