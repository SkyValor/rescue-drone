namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class DeliverySystem : Node
{
    public override void _Notification(int what) => this.Notify(what);

    [Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();

    public ushort Points { get; private set; }

    public void OnResolved()
    {
        GameRepo.DronesDelivered += OnDeliverySuccessful;
    }

    public void OnExitTree()
    {
        GameRepo.DronesDelivered -= OnDeliverySuccessful;
    }

    private void OnDeliverySuccessful(SmallDrone[] drones)
    {
        Points += (ushort) drones.Length;
    }
    
}
