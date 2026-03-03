namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyLogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : State, IGet<Input.PhysicsTick>
        {
            public Transition On(in Input.PhysicsTick input) => To<Patrol>();
        }
    }
}