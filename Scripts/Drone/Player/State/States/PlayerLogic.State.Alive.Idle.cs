namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : Alive, IGet<Input.OnAfterPhysicsTick>, IGet<Input.StartedMoving>
        {
            public Idle()
            {
                this.OnEnter(() => Output(new Output.ToggleBobEffect(IsBobbing: true)));
                this.OnExit(() => Output(new Output.ToggleBobEffect(IsBobbing: false)));
            }

            public Transition On(in Input.OnAfterPhysicsTick input)
            {
                var settings = Get<PlayerSettings>();

                var isMoving = Get<PlayerTestScript>().IsMoving();
                var wasNotMoving = Get<Data>().WasNotMoving(settings.StoppingSpeed);
                
                if (isMoving && wasNotMoving) Input(new Input.StartedMoving());

                return ToSelf();
            }
            
            public Transition On(in Input.StartedMoving input) => To<Moving>();
        }
    }
}
