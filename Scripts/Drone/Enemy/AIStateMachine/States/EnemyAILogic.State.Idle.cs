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
                var player = Get<IGameRepo>().Player.Value;
                if (player is null) return ToSelf();
                
                var sight = Get<SightSensor>();
                return sight.TargetInSight(player) ? To<Pursuit>() : To<MovingToCircuit>();
            }
        }
    }
}
