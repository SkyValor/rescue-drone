namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

public interface IPlayerDrone : IFlyingDrone
{
    
}

[Meta(typeof(IAutoNode))]
public partial class PlayerDrone : CharacterBody3D, IPlayerDrone
{
    public override void _Notification(int what) => this.Notify(what);
    
    
}
