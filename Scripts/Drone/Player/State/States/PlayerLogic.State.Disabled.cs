namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Disabled : State, IGet<Input.Enable>
        {
            public Disabled()
            {
                OnAttach(() => Get<IGameRepo>().LevelStart += OnLevelStart);
                OnDetach(() => Get<IGameRepo>().LevelStart -= OnLevelStart);
            }

            private void OnLevelStart()
            {
                Output(new Output.ToggleMouseCapture());
                Input(new Input.Enable());
            }
            
            public Transition On(in Input.Enable input) => To<Idle>();
        }
    }
}
