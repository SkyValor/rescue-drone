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
	
	public PlayerLogic StateMachine { get; private set; }
	private PlayerLogic.IBinding Binding { get; set; }
	
	private bool isBobbing;

	public void OnResolved()
	{
		StateMachine = new PlayerLogic();
		StateMachine.Set(this);
		StateMachine.Set(Settings);
		StateMachine.Set(GameRepo);

		Binding = StateMachine.Bind();
		Binding.Handle((in PlayerLogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		Binding.Handle((in PlayerLogic.Output.RotationComputed output) => GlobalRotation = output.GlobalRotation);
		Binding.Handle((in PlayerLogic.Output.ToggleMouseCapture _) => ToggleMouseCapture());
		Binding.Handle((in PlayerLogic.Output.ToggleBobEffect output) => isBobbing = output.IsBobbing);

		StateMachine.Start();
	}

	public void OnExitTree()
	{
		Binding.Dispose();
		StateMachine.Stop();
	}

	public override void _Input(InputEvent @event)
	{
		if (!GameRepo.PlayerInControl.Value) return;
		if (StateMachine is null || !StateMachine.IsStarted) return;
		
		StateMachine.Input(new PlayerLogic.Input.OnInputEvent(@event));
	}

	public override void _PhysicsProcess(double delta)
	{
		ApplyBobEffect();
		
		if (StateMachine is null || !StateMachine.IsStarted) return;
		
		StateMachine.Input(new PlayerLogic.Input.OnPhysicsTick(delta));
		MoveAndSlide();
	}

	public Vector3 GetInputBasedOnCamera(Camera3D camera)
	{
		if (camera is null)
		{
			GD.PrintErr("Parameter 'camera' is null. Player drone cannot get input based on camera.");
			return Vector3.Zero;
		}
		
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

	public float GetVerticalInput()
	{
		return Input.GetAxis(GameInputs.ThrottleDown, GameInputs.ThrottleUp);
	}

	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
	private void ApplyBobEffect()
	{
		if (isBobbing)
		{
			// Apply a subtle idle bob up and down
			var bobOffset = Mathf.Sin(Time.GetTicksMsec() * 0.001f * Settings.HoverBobFrequency) * Settings.HoverBobAmplitude;
			var meshPosition = DroneModel.Position;
			meshPosition.Y = Mathf.Lerp(meshPosition.Y, bobOffset, 0.1f);
			DroneModel.Position = meshPosition;
		}
		else
		{
			// Return to local origin smoothly
			var meshPosition = DroneModel.Position;
			meshPosition.Y = Mathf.Lerp(meshPosition.Y, 0.0f, 0.1f);
			DroneModel.Position = meshPosition;
		}
	}
	
}
