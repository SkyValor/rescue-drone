namespace RescueDrone;

using System;
using System.Diagnostics.CodeAnalysis;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IEnemyAILogic : ILogicBlock<EnemyAILogic.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class EnemyAILogic : LogicBlock<EnemyAILogic.State>, IEnemyAILogic
{
    public override Transition GetInitialState() => To<State.Disabled>();
    
    [Meta]
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    public partial record State : StateLogic<State>;

    protected override void HandleError(Exception e)
    {
        base.HandleError(e);
        DebugConsole.PrintError("Error in state: " + Value);
        throw e;
    }

    public static class Input
    {
        public readonly record struct ReturnToIdle;
        
        public readonly record struct Enable;
        public readonly record struct PhysicsTick(double Delta);
        public readonly record struct Moved;
        
        public readonly record struct PlayerDetected(PlayerDrone Player);
        public readonly record struct PlayerLost;

        public readonly record struct MoveToCircuit;
        public readonly record struct MoveToWaypoint;
        
        public readonly record struct StartScanning;
        public readonly record struct StartAlertScanning;
        public readonly record struct ScanningComplete;

        public readonly record struct PlayerDroneCloseEnough;
        public readonly record struct PlayerDroneTooClose;
        public readonly record struct PlayerDroneTooFar;
    }
    
    public static class Output
    {
        public readonly record struct RotationComputed(Transform3D GlobalTransform);
        public readonly record struct VelocityComputed(Vector3 Velocity);
    }
}
