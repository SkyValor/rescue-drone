namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class PlayerCameraLogic : LogicBlock<PlayerCameraLogic.State>
{
    [Meta]
    public partial record State : StateLogic<State>;

    public override Transition GetInitialState() => To<State.Disabled>();

    public static class Input
    {
        public readonly record struct Enable;
        public readonly record struct Disable;
        public readonly record struct OnInputEvent(InputEvent Event);
    }

    public static class Output
    {
        public readonly record struct RotationComputed(Vector3 Rotation);
        public readonly record struct ZoomComputed(float Length);
    }
    
}