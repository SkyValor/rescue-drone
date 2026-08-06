namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class AppLogic
{
    public partial record State
    {
        [Meta]
        public partial record LeavingMainMenu : State, IGet<Input.FadeOutFinished>
        {
            public LeavingMainMenu()
            {
                this.OnEnter(() => Output(new Output.TransitionToBlack()));
            }

            public Transition On(in Input.FadeOutFinished input) => To<InGame>();
        }
    }
}
