namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Disabled : State, IGet<Input.Enable>
        {
            public Transition On(in Input.Enable input) => To<Idle>();
        }
    }
}
