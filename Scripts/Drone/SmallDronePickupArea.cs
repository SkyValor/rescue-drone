namespace RescueDrone;

using Godot;

public partial class SmallDronePickupArea : SmallDroneReactArea
{
	[Export] private SmallDrone SmallDrone { get; set; }
	[Export] private float TimeToPickup { get; set; } = 3.5f;

	public override void _Ready()
	{
		base._Ready();
		TimeToAction = TimeToPickup;
		DebugColor = Colors.Yellow;
	}

	protected override void OnCountdownTimeout()
	{
		DroneFormation.AddDrone(SmallDrone);
		QueueFree();
	}
	
}
