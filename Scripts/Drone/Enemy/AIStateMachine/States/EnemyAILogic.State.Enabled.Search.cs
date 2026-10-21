namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Search : Enabled, IGet<Input.PlayerDetected>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                DrawPlayerDetectionLine(false);
                
                DebugDraw3D.DrawLine(
                    a: Get<EnemyAIDrone>().GlobalPosition,
                    b: Get<Data>().DetectedPlayer.GlobalPosition,
                    color: Colors.Goldenrod);
                
                return ToSelf();
            }
            
            public Transition On(in Input.PlayerDetected input) => To<Pursuit>();
        }
    }
}
