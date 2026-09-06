namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Disabled : State, IGet<Input.Enable>
        {
            public Transition On(in Input.Enable input) => throw new System.NotImplementedException();
        }
    }
}
