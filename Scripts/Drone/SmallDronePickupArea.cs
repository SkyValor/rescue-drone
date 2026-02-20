namespace RescueDrone;

using Godot;

public partial class SmallDronePickupArea : Area3D
{
	[Export] private SmallDrone SmallDrone { get; set; }
	[Export] private float TimeToPickup { get; set; } = 3.5f;

	private DroneFormation droneFormation;
	private float elapsedTime;
	private Timer countdownToPickup;
	private float areaRadius;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		var collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		if (collisionShape?.Shape is SphereShape3D sphere) 
			areaRadius = sphere.Radius;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;
		BodyExited -= OnBodyExited;

		if (countdownToPickup is not null)
			countdownToPickup.Timeout -= OnCountdownTimeout;
	}

	public override void _Process(double delta)
	{
		DebugDraw3D.DrawSphere(GlobalPosition, areaRadius, Colors.Yellow);
	}

	private void OnBodyEntered(Node3D other)
	{
		if (other is not Drone player)
			return;

		droneFormation = player.DroneFormation;
		StartCountdown();
	}

	private void OnBodyExited(Node3D other)
	{
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
