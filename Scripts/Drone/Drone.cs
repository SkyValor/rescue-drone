namespace RescueDrone;

using Godot;

public partial class Drone : CharacterBody3D
{
	[Export] public DroneController Controller { get; set; }
	[Export] public DroneEnergy Energy { get; set; }
	[Export] private DroneMovement Movement { get; set; }
	[Export] private DroneRotationHandler RotationHandler { get; set; }

	[Export] private bool MovementEnabled { get; set; } = true;
	[Export] private bool RotationEnabled { get; set; } = true;
	
	public override void _Ready()
	{
		base._Ready();
		if (MovementEnabled)
			Movement?.SetDrone(this);
		if (RotationEnabled)
			RotationHandler?.SetDrone(this);

		if (Controller is null)
			return;
		
		Controller.PitchInput += OnPitchInput;
		Controller.RollInput += OnRollInput;
		Controller.YawInput += OnYawInput;
		Controller.ThrottleInput += OnThrottleInput;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		var fDelta = (float) delta;
		Controller?.Tick();
		
		if (MovementEnabled)
			Movement?.Tick(fDelta);
		if (RotationEnabled)
			RotationHandler?.Tick(fDelta);
	}

	private void OnPitchInput(float input)
	{
		if (MovementEnabled)
			Movement?.SetPitchIntent(input);
		if (RotationEnabled)
			RotationHandler?.SetPitch(input);
	}

	private void OnRollInput(float input)
	{
		if (MovementEnabled)
			Movement?.SetRollIntent(input);
		if (RotationEnabled)
			RotationHandler?.SetRoll(input);
	}

	private void OnYawInput(float input)
	{
		if (MovementEnabled)
			Movement?.SetYawIntent(input);
		if (RotationEnabled)
			RotationHandler?.SetYaw(input);
	}

	private void OnThrottleInput(float input)
	{
		if (MovementEnabled)
			Movement?.SetThrottleIntent(input);
		if (RotationEnabled)
			RotationHandler?.SetThrottle(input);
	}

}
