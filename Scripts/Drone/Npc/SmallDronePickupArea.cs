namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class SmallDronePickupArea : SmallDroneReactArea
{
	public override void _Notification(int what) => this.Notify(what);
	
	[Export] private SmallDrone SmallDrone { get; set; }
	[Export] private float TimeToPickup { get; set; } = 3.5f;

	public override void OnReady()
	{
		base.OnReady();
		TimeToAction = TimeToPickup;
		DebugColor = Colors.Yellow;
	}

	protected override void OnCountdownTimeout()
	{
		DroneFormation.AddDrone(SmallDrone);
		GameRepo?.InvokeDronePickedUp(SmallDrone);
		QueueFree();
	}
	
}
