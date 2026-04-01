namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class EnemyBullet : RigidBody3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] public float Speed { get; private set; }

    public void OnReady()
    {
        ApplyImpulse(-GlobalTransform.Basis.Z * Speed);
    }

    public void OnEnterTree()
    {
        BodyEntered += OnBodyEntered;
    }

    public void OnExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not Drone player)
            return;

        GD.Print("EnemyBullet collision with player drone!");
        player.Energy.DepleteEnergy(10);
        QueueFree();
    }
}
