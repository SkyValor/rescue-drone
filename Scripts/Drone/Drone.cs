namespace RescueDrone;

using Godot;

public partial class Drone : CharacterBody3D
{
	[Export] public DroneController Controller { get; set; }
	[Export] public DroneEnergy Energy { get; set; }
	[Export] private DroneMovement Movement { get; set; }
	[Export] private DroneRotationHandler RotationHandler { get; set; }
	[Export] public DroneFormation DroneFormation { get; set; }
	[Export] public Node3D CameraTarget { get; set; }
	[Export] public Node3D CameraRig { get; set; }

	[Export] private float CameraForwardOffset { get; set; } = 3.0f;
	[Export] private float CameraHeightOffset { get; set; } = 1.0f;
	
	[Export] private float CameraTurnInfluence { get; set; } = 15.0f;
	[Export] private float CameraFollowSpeed { get; set; } = 5.0f;
	
	[Export] public float LookForwardOffset { get; set; } = 5f;
	[Export] public float CameraDistance { get; set; } = 8f;
	[Export] public float CameraHeight { get; set; } = 2f;

	[Export] private float CameraRotatingLag { get; set; } = 3f;
	[Export] private float CameraPositionLag { get; set; } = 5f;
	
	[Export] private bool MovementEnabled { get; set; } = true;
	[Export] private bool RotationEnabled { get; set; } = true;

	private float cameraYaw;
	private float currentTurnInfluence;
	
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

		//HandleCamera(deltaTime);
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
		
		var back = new Basis(Vector3.Up, cameraYaw).Z;
		var desiredPosition = 
			GlobalPosition + 
			back * CameraDistance + 
			Vector3.Up * CameraHeight;
		
		CameraRig.GlobalPosition = CameraRig.GlobalPosition.Lerp(desiredPosition, CameraPositionLag * delta);
		CameraRig.LookAt(CameraTarget.GlobalPosition, Vector3.Up);
	}

	private void HandleCamera(float delta)
	{
		if (CameraTarget is null)
			return;
		
		var turnInput = Input.GetActionStrength("turn_right") - Input.GetActionStrength("turn_left");
		var targetInfluence = turnInput * CameraTurnInfluence;
		currentTurnInfluence = Mathf.Lerp(currentTurnInfluence, targetInfluence, CameraFollowSpeed * delta);

		var forward = -GlobalTransform.Basis.Z;
		var targetPosition = 
			GlobalTransform.Origin + 
			forward * CameraForwardOffset + 
			Vector3.Up * CameraHeightOffset;

		var extraYaw = new Basis(Vector3.Up, Mathf.DegToRad(currentTurnInfluence));
		var finalBasis = GlobalTransform.Basis * extraYaw;

		CameraTarget.GlobalTransform = new Transform3D(finalBasis, targetPosition);
		GD.Print($"CameraTarget position ({targetPosition})");
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
