namespace RescueDrone.States;

using Chickensoft.Introspection;

public partial class GameLogic
{
    public partial record State
    {
        [Meta]
        public partial record Loading : State
        {
            
        }
    }
}
