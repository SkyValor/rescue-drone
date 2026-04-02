namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyLogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : State, IGet<Input.PhysicsTick>
        {
            public Transition On(in Input.PhysicsTick input)
            {
                Godot.GD.Print("Idle");
                return HasLineOfSight(Get<EnemyDrone>(), Get<Drone>(), Get<Settings>()) 
                    ? To<Attack>() 
                    : To<Patrol.GoToWaypoint>();
            }
        }
    }
}
