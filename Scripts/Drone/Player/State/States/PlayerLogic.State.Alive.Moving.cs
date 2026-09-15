namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Moving : Alive, IGet<Input.AfterMove>, IGet<Input.StoppedMoving>
        {
            public Moving()
            {
                this.OnEnter(() => Output(new Output.ToggleTiltEffect(IsTilting: true)));
                this.OnExit(() => Output(new Output.ToggleTiltEffect(IsTilting: false)));
            }
            
            public Transition On(in Input.AfterMove input)
            {
                var settings = Get<PlayerSettings>();
                
                var isNotMoving = !Get<PlayerDrone>().IsMoving();
                var wasMoving = Get<Data>().WasMoving(settings.StoppingSpeed);

                if (wasMoving && isNotMoving) Input(new Input.StoppedMoving());

                return ToSelf();
            }

            public Transition On(in Input.StoppedMoving input) => To<Idle>();
        }
    }
}
