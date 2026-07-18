namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class AppLogic
{
    public partial record State
    {
        [Meta]
        public partial record InMainMenu : State, IGet<Input.NewGame>
        {
            public InMainMenu()
            {
                this.OnEnter(() =>
                {
                    Get<IAppRepo>().OnMainMenuEnter();
                    Output(new Output.ShowMainMenu());
                });
            }

            public Transition On(in Input.NewGame input) => To<LeavingMainMenu>();

        }
    }
}

