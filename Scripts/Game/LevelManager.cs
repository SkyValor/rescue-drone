namespace RescueDrone.Scripts.Core;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class LevelManager : Node3D
{
    public override void _Notification(int what) => this.Notify(what);
    
    // [Export] public EnemyDrone Enemy { get; set; }

    public void OnReady()
    {
        
    }
    
}
