namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class PlayerTestScript : CharacterBody3D
{
	public override void _Notification(int what) => this.Notify(what);

	[Export] public PlayerSettings Settings { get; private set; } = new();
	[Export] public Node3D DroneModel { get; private set; }

	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
	
	public override void _Input(InputEvent @event)
	{
		if (!GameRepo.PlayerInControl.Value) return;
		
		if (@event.IsActionPressed(GameInputs.ToggleMouseCapture)) ToggleMouseCapture();
	}

	public override void _PhysicsProcess(double delta)
	{
		var direction = Vector3.Zero;
		var directionVertical = 0f;
		
		var camera = GameRepo.MainCamera.Value;
		if (GameRepo.PlayerInControl.Value)
		{
			direction = GetInputBasedOnCamera(camera);
			directionVertical = GetVerticalInput();
		}

		var deltaTime = (float) delta;
		Velocity = ComputeVelocity(direction, directionVertical, deltaTime);
		AlignDroneNoseWithCamera(camera, deltaTime);
		MoveAndSlide();
	}

	private static Vector3 GetInputBasedOnCamera(Camera3D camera)
	{
		var cameraBasis = camera.Basis;
		var rawInput = Input.GetVector(
			GameInputs.MoveLeft, GameInputs.MoveRight, 
			GameInputs.MoveForward, GameInputs.MoveBack);

		// This is to ensure that diagonal input isn't stronger than axis aligned input.
		var input = new Vector3
		{
			X = rawInput.X * Mathf.Sqrt(1f - (rawInput.Y * rawInput.Y / 2f)),
			Z = rawInput.Y * Mathf.Sqrt(1f - (rawInput.X * rawInput.X / 2f))
		};

		return cameraBasis * input with { Y = 0f };
	}

	private static float GetVerticalInput()
	{
		return Input.GetAxis(GameInputs.ThrottleDown, GameInputs.ThrottleUp);
	}

	private Vector3 ComputeVelocity(Vector3 direction, float verticalDirection, float deltaTime)
	{
		var velocity = Velocity;
		if (direction != Vector3.Zero)
		{
			// Accelerate the velocity until max speed
			direction *= Settings.MaxSpeed;
			velocity.X = Mathf.MoveToward(velocity.X, direction.X, Settings.Acceleration * deltaTime);
			velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z, Settings.Acceleration * deltaTime);
		}
		else
		{
			// Decelerate the velocity until zero
			velocity.X = Mathf.MoveToward(velocity.X, 0f, Settings.Acceleration * deltaTime);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0f, Settings.Acceleration * deltaTime);
		}

		if (Mathf.IsEqualApprox(verticalDirection, 0f))
		{
			velocity.Y = Mathf.MoveToward(velocity.Y, 0f, Settings.VerticalAcceleration * deltaTime);
		}
		else
		{
			verticalDirection *= Settings.MaxVerticalSpeed;
			velocity.Y = Mathf.MoveToward(velocity.Y, verticalDirection, Settings.VerticalAcceleration * deltaTime);
		}

		return velocity;
	}

	private void AlignDroneNoseWithCamera(Camera3D camera, float deltaTime)
	{
		var targetRotationY = camera.GlobalRotation.Y;
		GlobalRotation = GlobalRotation with
		{
			Y = Mathf.RotateToward(GlobalRotation.Y, targetRotationY, Settings.RotationSpeed * deltaTime)
		};
	}
	
	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
}
