namespace RescueDrone;

using Godot;

public partial class Drone : CharacterBody3D
{
	[Export] public DroneController Controller { get; set; }
	[Export] public DroneEnergy Energy { get; set; }
	[Export] public Node3D CameraTarget { get; set; }
	[Export] public Node3D CameraRig { get; set; }
	[Export] public DroneFormation DroneFormation { get; set; }
	[Export] private DroneMovement Movement { get; set; }
	[Export] private DroneRotationHandler RotationHandler { get; set; }

	[Export] public float LookForwardOffset { get; set; } = 5f;
	[Export] public float CameraDistance { get; set; } = 8f;
	[Export] public float CameraHeight { get; set; } = 2f;

	[Export] private float CameraRotatingLag { get; set; } = 3f;
	[Export] private float CameraPositionLag { get; set; } = 5f;

	[Export] private float FOVWhenAlone { get; set; } = 75f;
	[Export] private float FOVWhenFollowed { get; set; } = 100f;
	
	[Export] private bool MovementEnabled { get; set; } = true;
	[Export] private bool RotationEnabled { get; set; } = true;

	private float cameraYaw;
	
	public override void _Ready()
	{
		base._Ready();
		cameraYaw = Rotation.Y;
		
		if (MovementEnabled)
			Movement?.SetDrone(this);
		if (RotationEnabled)
		{
			RotationHandler?.SetDrone(this);
			RotationHandler?.SetDroneBody(GetNode<Node3D>("%DroneBody"));
		}

		if (Controller is null)
			return;
		
		Controller.PitchInput += OnPitchInput;
		Controller.RollInput += OnRollInput;
		Controller.YawInput += OnYawInput;
		Controller.ThrottleInput += OnThrottleInput;
	}

	public override void _ExitTree()
	{
		if (Controller is null)
			return;
		
		Controller.PitchInput -= OnPitchInput;
		Controller.RollInput -= OnRollInput;
		Controller.YawInput -= OnYawInput;
		Controller.ThrottleInput -= OnThrottleInput;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		if (CameraTarget is not null)
			DebugDraw3D.DrawSphere(CameraTarget.GlobalPosition, 0.5f, Colors.BlueViolet);
		
		var forward = -GlobalTransform.Basis.Z;
		var forwardPoint = GlobalPosition + forward * 5f;
		DebugDraw3D.DrawBox(forwardPoint, Quaternion.Identity, Vector3.One, Colors.Red, true);
		
		var deltaTime = (float) delta;
		Controller?.Tick();
		
		if (MovementEnabled)
			Movement?.Tick(deltaTime);
		if (RotationEnabled)
			RotationHandler?.Tick(deltaTime);

		UpdateLookTarget();
		UpdateCamera(deltaTime);
	}

	private void UpdateLookTarget()
	{
		var forward = -GlobalTransform.Basis.Z;
		CameraTarget.GlobalPosition = GlobalPosition + forward * LookForwardOffset;
	}

	private void UpdateCamera(float delta)
	{
		var droneYaw = Rotation.Y;
		cameraYaw = Mathf.LerpAngle(cameraYaw, droneYaw, CameraRotatingLag * delta);
		
		var turnInput = Input.GetActionStrength("turn_right") - Input.GetActionStrength("turn_left");
		var extraYaw = turnInput * Mathf.DegToRad(15f);
		var finalYaw = cameraYaw + extraYaw;
		
		var back = new Basis(Vector3.Up, finalYaw).Z;
		var desiredPosition = 
			GlobalPosition + 
			back * CameraDistance + 
			Vector3.Up * CameraHeight;
		
		CameraRig.GlobalPosition = CameraRig.GlobalPosition.Lerp(desiredPosition, CameraPositionLag * delta);
		CameraRig.LookAt(CameraTarget.GlobalPosition, Vector3.Up);
	}

	// TODO: Create a central repository for signals. Decouple this class from any camera.
	// Make this class call the signal repo and alert about the change of followers.
	// The PhantomCamera reaction should listen for that signal and increase/decrease the FOV value.
	
	private void OnFollowersChanged(ushort followers)
	{
		if (followers == 0)
		{
			
		}
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
	}

	private void OnThrottleInput(float input)
	{
		if (MovementEnabled)
			Movement?.SetThrottleIntent(input);
	}

}
