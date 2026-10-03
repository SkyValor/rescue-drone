namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class PlayerDrone : CharacterBody3D, IFlyingDrone, IProvide<PlayerLogic>, IProvide<DroneModel>
{
	public override void _Notification(int what) => this.Notify(what);

	[Export] public PlayerSettings Settings { get; private set; }
	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
	
	[Node] public DroneFormation Formation { get; private set; }
	[Node] private DroneModel Model { get; set; }

	PlayerLogic IProvide<PlayerLogic>.Value() => StateMachine;
	DroneModel IProvide<DroneModel>.Value() => Model;
	
	public PlayerLogic StateMachine { get; private set; }
	private PlayerLogic.IBinding Binding { get; set; }
	
	private bool isBobbing;
	private float bobbingTime;

	public void OnResolved()
	{
		StateMachine = new PlayerLogic();
		StateMachine.Set(new PlayerLogic.Data());
		StateMachine.Set(this);
		StateMachine.Set(Settings);
		StateMachine.Set(GameRepo);
		this.Provide();

		Binding = StateMachine.Bind();
		Binding.Handle((in PlayerLogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		Binding.Handle((in PlayerLogic.Output.RotationComputed output) => GlobalRotation = output.GlobalRotation);
		Binding.Handle((in PlayerLogic.Output.MoveDirectionTilt output) => OnMoveDirectionTilt(output.InputDirection, output.Delta));
		Binding.Handle((in PlayerLogic.Output.ToggleMouseCapture _) => ToggleMouseCapture());

		StateMachine.Start();
	}

	public void OnExitTree()
	{
		Binding.Dispose();
		StateMachine.Stop();
	}

	public override void _Input(InputEvent @event)
	{
		if (StateMachine is null || !StateMachine.IsStarted) return;
		
		StateMachine.Input(new PlayerLogic.Input.OnInputEvent(@event));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (StateMachine is null || !StateMachine.IsStarted) return;
		
		StateMachine.Input(new PlayerLogic.Input.OnPhysicsTick(delta));
		MoveAndSlide();

		StateMachine.Input(new PlayerLogic.Input.AfterMove());
	}

	public bool IsMoving() => Velocity.Length() >= Settings.StoppingSpeed;

	public Vector2 GetInputDirection()
	{
		return Input.GetVector(GameInputs.KbMoveLeft, GameInputs.KbMoveRight, GameInputs.KbMoveForward, GameInputs.KbMoveBack);
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
			GameInputs.KbMoveLeft, GameInputs.KbMoveRight, 
			GameInputs.KbMoveForward, GameInputs.KbMoveBack);

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
		return Input.GetAxis(GameInputs.KbDescend, GameInputs.KbAscend);
	}

	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
	// TODO: Encapsulate the movement tilt effect in its own class TiltComponent.
	// TODO: Create a data class TiltSettings to hold configurations used by this component.

	private void OnMoveDirectionTilt(Vector2 inputDirection, double delta)
	{
		if (inputDirection.IsEqualApprox(Vector2.Zero) && Model.Rotation.IsEqualApprox(Vector3.Zero))
			return;

		var targetRotation = new Vector3
		{
			X = Mathf.DegToRad(inputDirection.Y * Settings.MaxTiltAngleDegrees),
			Y = 0f,
			Z = Mathf.DegToRad(-inputDirection.X * Settings.MaxTiltAngleDegrees)
		};

		Model.Rotation = Model.Rotation.MoveToward(targetRotation, Settings.TiltLerpSpeed * (float) delta);
	}
	
}
