namespace RescueDrone;

using Godot;

public partial class DroneRotationHandler : Node
{
	[Export] private float PitchMaxDegrees { get; set; }
	[Export] private float PitchDelta { get; set; } = 1f;
	
	[Export] private float RollMaxDegrees { get; set; }
	[Export] private float RollDelta { get; set; } = 1f;
	
	private Drone drone;
	private Node3D body;
	private float inputPitch;
	private float currentPitch;
	private float inputRoll;
	private float currentRoll;

	public void SetDrone(Drone drone) => this.drone = drone;
	public void SetDroneBody(Node3D body) => this.body = body;
	
	public void SetPitch(float pitch) => inputPitch = pitch;
	public void SetRoll(float roll) => inputRoll = roll;
	
	public void Tick(float delta)
	{
		UpdateRotationForce(ref currentPitch, inputPitch, PitchDelta);
		currentPitch = Mathf.MoveToward(currentPitch, inputPitch, PitchDelta * delta);
		
		UpdateRotationForce(ref currentRoll, inputRoll, RollDelta);
		currentRoll = Mathf.MoveToward(currentRoll, inputRoll, RollDelta * delta);

		body.RotationDegrees = drone.RotationDegrees with
		{
			X = currentPitch * PitchMaxDegrees,
			Z = currentRoll * RollMaxDegrees
		};
	}

	private static void UpdateRotationForce(ref float currentForce, float inputForce, float forceDelta)
	{
		if ((currentForce < 0f && inputForce > 0f) || (currentForce > 0f && inputForce < 0f))
			currentForce = Mathf.MoveToward(currentForce, inputForce, forceDelta * 2f);
		else
			currentForce = Mathf.MoveToward(currentForce, inputForce, forceDelta);
	}
	
}
