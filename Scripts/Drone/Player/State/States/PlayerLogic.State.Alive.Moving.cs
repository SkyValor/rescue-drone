namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Moving : Alive
        {
            
        }
    }
}
