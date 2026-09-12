namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IEnemyAILogic : ILogicBlock<EnemyAILogic.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class EnemyAILogic : LogicBlock<EnemyAILogic.State>, IEnemyAILogic
{
    public override Transition GetInitialState() => To<State.Disabled>();
    
    public static class Input
    {
        public readonly record struct ReturnToIdle;
        
        public readonly record struct Enable;
        public readonly record struct PhysicsTick(double Delta);
        public readonly record struct Moved;

        public readonly record struct MoveToCircuit;
        public readonly record struct MoveToWaypoint;
        public readonly record struct StartScanning;

        public readonly record struct PlayerDetected(PlayerDrone Player);
        
        public readonly record struct PlayerDroneCloseEnough;
        public readonly record struct PlayerDroneTooClose;
        public readonly record struct PlayerDroneTooFar;
        
        public readonly record struct InitiateRotatingLeft;
        public readonly record struct InitiateRotatingRight;
        public readonly record struct FinishedLookout;
    }
    
    public static class Output
    {
        public readonly record struct RotationComputed(Transform3D GlobalTransform);
        public readonly record struct VelocityComputed(Vector3 Velocity);
    }
}
