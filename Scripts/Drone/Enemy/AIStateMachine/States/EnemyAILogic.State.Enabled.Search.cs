namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Search : Enabled, IGet<Input.PlayerDetected>
        {
            public Transition On(in Input.PlayerDetected input) => To<Pursuit>();
        }
    }
}
