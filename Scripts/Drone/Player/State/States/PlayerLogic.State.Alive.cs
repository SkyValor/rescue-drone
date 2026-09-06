namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Alive : State
        {
            
        }
    }
}
