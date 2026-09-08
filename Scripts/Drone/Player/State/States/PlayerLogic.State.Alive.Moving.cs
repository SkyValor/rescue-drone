namespace RescueDrone;

using Chickensoft.Introspection;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public partial record Moving : Alive, IGet<Input.OnAfterPhysicsTick>, IGet<Input.StoppedMoving>
        {
            public Transition On(in Input.OnAfterPhysicsTick input)
            {
                var settings = Get<PlayerSettings>();
                
                var isNotMoving = !Get<PlayerTestScript>().IsMoving();
                var wasMoving = Get<Data>().WasMoving(settings.StoppingSpeed);

                if (wasMoving && isNotMoving) Input(new Input.StoppedMoving());

                return ToSelf();
            }

            public Transition On(in Input.StoppedMoving input) => To<Idle>();
        }
    }
}
