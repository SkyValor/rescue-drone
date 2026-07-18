namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class DroneMovePhysics : Node
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public float MaxSpeed { get; private set; } = 15f;
    [Export] public float Acceleration { get; private set; } = 20f;
    [Export] public float Deceleration { get; private set; } = 12f;
    [Export] public float RotationSpeed { get; private set; } = 3f;
    [Export] public float MinTurnSpeedPercentage { get; private set; } = 0.25f;

    public void OnPhysicsProcess(double delta)
    {
        
    }
}
