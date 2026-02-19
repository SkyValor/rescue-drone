namespace RescueDrone;

using Godot;

public partial class SmallDronePickupArea : Area3D
{
	[Export] private SmallDrone SmallDrone { get; set; }
	[Export] private float TimeToPickup { get; set; } = 3.5f;

	private DroneFormation droneFormation;
	private float elapsedTime;
	private Timer countdownToPickup;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;
		BodyExited -= OnBodyExited;

		if (countdownToPickup is not null)
			countdownToPickup.Timeout -= OnCountdownTimeout;
	}

	private void OnBodyEntered(Node3D other)
	{
		GD.Print("OnAreaEntered");
		if (other is not Drone player)
			return;

		droneFormation = player.DroneFormation;
		StartCountdown();
	}

	private void OnBodyExited(Node3D other)
	{
		GD.Print("OnAreaExited");
		if (other is not Drone player || droneFormation != player.DroneFormation)
			return;

		droneFormation = null;
		StopCountdown();
	}

	private void StartCountdown()
	{
		if (countdownToPickup is null)
		{
			countdownToPickup = new Timer();
			AddChild(countdownToPickup);
			countdownToPickup.Timeout += OnCountdownTimeout;
		}
		else
		{
			countdownToPickup.Stop();
		}
		
		countdownToPickup.Start(TimeToPickup);
	}

	private void StopCountdown()
	{
		countdownToPickup?.Stop();
	}

	private void OnCountdownTimeout()
	{
		droneFormation.AddDrone(SmallDrone);
		QueueFree();
	}
	
}
