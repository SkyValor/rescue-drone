namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The Idle state is responsible for deciding whether the enemy drone changes its state
        /// to Pursuit if there is sight of a player drone or Patrolling otherwise.
        /// </summary>
        [Meta]
        public partial record Idle : State, IGet<Input.PhysicsTick>
        {
            public Transition On(in Input.PhysicsTick input)
            {
                var player = Get<IGameRepo>().PlayerDrone.Value;
                if (player is null) return ToSelf();

                var data = Get<Data>();
                var sight = Get<SightSensor>();
                if (sight.TargetInSight(player) && data.CanEnterPatrol())
                    return To<Pursuit>();

                // if (data.CanTransitionToPatrol())
                    // return To<MovingToCircuit>();

                return ToSelf();
                // return sight.TargetInSight(player) ? To<Pursuit>() : To<MovingToCircuit>();
            }
        }
    }
}
