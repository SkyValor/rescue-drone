namespace RescueDrone;

using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public record Idle : State, IGet<Input.PhysicsTick>
        {
            public Transition On(in Input.PhysicsTick input)
            {
                var player = Get<Drone>();
                if (PlayerIsInLineOfSight(Get<EnemyDrone>(), player, Get<Settings>()))
                {
                    Get<Data>().LastPlayerKnownPosition = player.GlobalPosition;
                    return To<Attack>();
                }

                return To<Patrol>();
            }
        }
    }
}
