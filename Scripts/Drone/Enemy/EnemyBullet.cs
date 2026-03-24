namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class EnemyBullet : RigidBody3D
{
    public override void _Notification(int what) => this.Notify(what);
}
