namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : Alive
        {
            public Idle()
            {
                this.OnEnter(() => Output(new Output.ToggleBobEffect(IsBobbing: true)));
                this.OnExit(() => Output(new Output.ToggleBobEffect(IsBobbing: false)));
            }
        }
    }
}
