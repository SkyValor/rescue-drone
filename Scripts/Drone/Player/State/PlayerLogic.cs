namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IPlayerLogic : ILogicBlock<PlayerLogic.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class PlayerLogic : LogicBlock<PlayerLogic.State>, IPlayerLogic
{
    public override Transition GetInitialState() => To<State.Disabled>();

    public static class Input
    {
        public readonly record struct Enable;
        public readonly record struct OnInputEvent(InputEvent Event);
        public readonly record struct OnPhysicsTick(double Delta);
    }

    public static class Output
    {
        public readonly record struct VelocityComputed(Vector3 Velocity);
    }
    
}
