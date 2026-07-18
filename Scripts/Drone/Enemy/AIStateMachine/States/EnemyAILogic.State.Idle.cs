namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : State, IGet<Input.PhysicsTick>
        {
            public Transition On(in Input.PhysicsTick input)
            {
                var sight = Get<SightSensor>();
                var player = Get<IGameRepo>().Player.Value;
                if (player is null) return ToSelf();
                
                return sight.TargetInSight(player) ? To<Patrol>() : To<Chase>();
            }
        }
    }
}
