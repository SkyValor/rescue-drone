namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerCameraLogic
{
    public partial record State
    {
        [Meta]
        public partial record Disabled : State, IGet<Input.Enable>
        {
            public Disabled()
            {
                OnAttach(() =>
                {
                    var gameRepo = Get<IGameRepo>();
                    gameRepo.LevelStart += OnLevelStart;
                });
                OnDetach(() =>
                {
                    var gameRepo = Get<IGameRepo>();
                    gameRepo.LevelStart -= OnLevelStart;
                });
            }

            private void OnLevelStart() => Input(new Input.Enable());

            public Transition On(in Input.Enable input) => To<Enabled>();
        }
    }
}
