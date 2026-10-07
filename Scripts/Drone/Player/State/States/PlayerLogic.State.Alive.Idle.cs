namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Idle : Alive, IGet<Input.AfterMove>, IGet<Input.StartedMoving>
        {
            public Transition On(in Input.AfterMove input)
            {
                var settings = Get<PlayerSettings>();

                var isMoving = Get<PlayerDrone>().IsMoving();
                var wasNotMoving = Get<Data>().WasNotMoving(settings.StoppingSpeed);
                
                if (isMoving && wasNotMoving) Input(new Input.StartedMoving());

                return ToSelf();
            }
            
            public Transition On(in Input.StartedMoving input) => To<Moving>();
        }
    }
}
