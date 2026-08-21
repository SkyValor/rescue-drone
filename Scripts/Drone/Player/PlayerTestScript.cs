namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class PlayerTestScript : CharacterBody3D
{
	public override void _Notification(int what) => this.Notify(what);

	[Export] public float MaxSpeed { get; private set; } = 5f;
	[Export] public float Acceleration { get; private set; } = 25f;
	[Export] public float RotationSpeed { get; private set; } = 12f;
	
	[Export] public Node3D DroneModel { get; private set; }

	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
	
	// [Export] public Camera3D Camera { get; private set; }

	public override void _Input(InputEvent @event)
	{
		if (!GameRepo.PlayerInControl.Value) return;
		
		if (@event.IsActionPressed(GameInputs.ToggleMouseCapture)) ToggleMouseCapture();
	}

	public override void _PhysicsProcess(double delta)
	{
		var direction = Vector3.Zero;
		var camera = GameRepo.MainCamera.Value;
		if (GameRepo.PlayerInControl.Value)
		{
			var inputDir = Input.GetVector(GameInputs.MoveLeft, GameInputs.MoveRight, GameInputs.MoveForward, GameInputs.MoveBack);
			
			// Get the movement direction based on the camera's perspective
			direction = new Vector3(inputDir.X, 0f, inputDir.Y);
			direction = direction.Rotated(Vector3.Up, camera.GlobalRotation.Y);
			
			// Align the player drone's nose with the camera's perspective
			var targetRotationY = camera.GlobalRotation.Y;
			GlobalRotation = GlobalRotation with
			{
				Y = Mathf.RotateToward(GlobalRotation.Y, targetRotationY, RotationSpeed * (float) delta)
			};
		}
		
		var deltaTime = (float) delta;
		if (direction != Vector3.Zero)
		{
			// Accelerate the velocity until max speed
			direction *= MaxSpeed;
			var velocity = Velocity;
			velocity.X = Mathf.MoveToward(velocity.X, direction.X, deltaTime * Acceleration);
			velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z, deltaTime * Acceleration);
			Velocity = velocity;

			// if (Velocity.LengthSquared() >= 0.1f)
			// {
			// 	// Make the player drone look forward based on the camera's perspective
			//     var cameraDir = -camera.GlobalTransform.Basis.Z;
			//     cameraDir.Y = 0f;
			//     cameraDir = cameraDir.Normalized();
			//
			//     if (cameraDir.Length() > 0)
			//     {
			// 	    var droneForward = -GlobalTransform.Basis.Z;
			// 	    var lookAtDirection = droneForward.MoveToward(cameraDir, RotationSpeed * deltaTime);
			// 	    DroneModel.LookAt(lookAtDirection);
			//     }
			// }
		}
		else
		{
			// Decelerate the velocity until zero
			var velocity = Velocity;
			velocity.X = Mathf.MoveToward(velocity.X, 0f, deltaTime * Acceleration);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0f, deltaTime * Acceleration);
			Velocity = velocity;
		}

		MoveAndSlide();
	}
	
	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
}
