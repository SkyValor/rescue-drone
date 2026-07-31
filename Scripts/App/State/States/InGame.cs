namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class AppLogic
{
    public partial record State
    {
        [Meta]
        public partial record InGame : State
        {
            public InGame()
            {
                this.OnEnter(() =>
                {
                    Get<IAppRepo>().OnEnterGame();
                    Output(new Output.ShowGame());
                });
                this.OnExit(() => Output(new Output.HideGame()));
                
                OnAttach(() => Get<IAppRepo>().GameExited += OnGameExited);
                OnDetach(() => Get<IAppRepo>().GameExited -= OnGameExited);
            }

            private void OnGameExited() => Input(new Input.EndGame());
        }
    }
}
