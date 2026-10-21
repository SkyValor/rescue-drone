namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IEnemyAILogic : ILogicBlock<EnemyAILogic.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class EnemyAILogic : LogicBlock<EnemyAILogic.State>, IEnemyAILogic
{
    public override Transition GetInitialState() => To<State.Disabled>();
    
    [Meta]
    public partial record State : StateLogic<State>;

    public override void OnStart()
    {
        var sightSensor = Get<SightSensorCone3D>();
        sightSensor.BodySighted += OnBodySighted;
        sightSensor.BodyHidden += OnBodyHidden;
    }
    
    public override void OnStop()
    {
        var sightSensor = Get<SightSensorCone3D>();
        sightSensor.BodySighted -= OnBodySighted;
        sightSensor.BodyHidden -= OnBodyHidden;
    }

    private void OnBodySighted(Node3D body)
    {
        if (body is not PlayerDrone playerDrone || Value is State.Disabled) return;

        var data = Get<Data>();
        
        // We set the Data.LastRepathPosition here to avoid having
        // a default value the first time it's needed

        data.DetectedPlayer = playerDrone;
        // data.LastKnownPlayerPosition = playerDrone.GlobalPosition;
        data.LastRepathPosition = playerDrone.GlobalPosition;
        Input(new Input.PlayerDetected());
    }
    
    private void OnBodyHidden(Node3D body)
    {
        if (body is not PlayerDrone || Value is State.Disabled) return;
    
        Input(new Input.PlayerLost());
    }

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

        public readonly record struct MoveToCircuit;
        public readonly record struct MoveToWaypoint;
        public readonly record struct StartScanning;
        public readonly record struct StartAlertScanning;
        public readonly record struct ScanningComplete;

        public readonly record struct PlayerDetected;
        public readonly record struct PlayerLost;
        
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
