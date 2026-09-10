namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class SmallDroneDeliveryArea : SmallDroneReactArea
{
	public override void _Notification(int what) => this.Notify(what);
	
	[Export] private float TimeToDeliver { get; set; } = 1.5f;

	public override void OnReady()
	{
		base.OnReady();
		TimeToAction = TimeToDeliver;
		DebugColor = Colors.Aqua;
	}

	protected override void OnCountdownTimeout()
	{
		var drones = DroneFormation.GetFollowers().ToArray();
		foreach (var follower in drones)
		{
			DroneFormation.RemoveDrone(follower);
			follower.QueueFree();
		}
		
		GameRepo?.InvokeDronesDelivered(drones);
	}
	
}
