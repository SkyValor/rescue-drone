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
	
	[Export] public Node3D DroneModel { get; private set; }

	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();

	// [Export] public Camera3D Camera { get; private set; }

	public override void _Input(InputEvent @event)
	{
		if (!GameRepo.PlayerInControl.Value) return;
		
		if (@event.IsActionPressed("toggle_mouse_capture")) ToggleMouseCapture();
	}

	public override void _PhysicsProcess(double delta)
	{
		var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		var direction = new Vector3(inputDir.X, 0f, inputDir.Y);
		// direction = direction.Rotated(Vector3.Up, Camera.GlobalRotation.Y);

		if (direction != Vector3.Zero)
		{
			direction *= MaxSpeed;
			var velocity = Velocity;
			velocity.X = (float) Mathf.MoveToward(velocity.X, direction.X, delta * Acceleration);
			velocity.Z = (float) Mathf.MoveToward(velocity.Z, direction.Z, delta * Acceleration);
			Velocity = velocity;

			// if (Velocity.LengthSquared() >= 0.1f)
			// {
			//     var cameraDir = -Camera.GlobalTransform.Basis.Z;
			//     cameraDir.Y = 0f;
			//     cameraDir = cameraDir.Normalized();
			//     
			//     if (cameraDir.Length() > 0) 
			//         DroneModel.LookAt(GlobalPosition + cameraDir * 5f, Vector3.Up);
			// }
		}
		else
		{
			var velocity = Velocity;
			velocity.X = (float) Mathf.MoveToward(velocity.X, 0f, delta * Acceleration);
			velocity.Z = (float) Mathf.MoveToward(velocity.Z, 0f, delta * Acceleration);
			Velocity = velocity;
		}

		MoveAndSlide();
	}
	
	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
}
