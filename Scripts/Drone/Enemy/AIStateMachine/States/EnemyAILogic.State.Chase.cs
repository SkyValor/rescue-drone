namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Chase : State
        {
            public Transition On(in Input.Enable input) => throw new System.NotImplementedException();
        }
    }
}
