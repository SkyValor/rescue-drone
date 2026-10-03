namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class PlayerDrone : CharacterBody3D, IFlyingDrone, IProvide<IDroneRepo>
{
	public override void _Notification(int what) => this.Notify(what);

	[Export] public PlayerSettings Settings { get; private set; }
	[Dependency] private IGameRepo GameRepo => this.DependOn<IGameRepo>();
	
	[Node] public DroneFormation Formation { get; private set; }
	[Node] private Node3D DroneModel { get; set; }
	
	public PlayerLogic StateMachine { get; private set; }
	private PlayerLogic.IBinding Binding { get; set; }

	private DroneRepo DroneRepo { get; set; }
	IDroneRepo IProvide<IDroneRepo>.Value() => DroneRepo;
	
	public void OnResolved()
	{
		DroneRepo = new DroneRepo();
		DroneRepo.SetDroneModel(DroneModel);
		this.Provide();
		
		StateMachine = new PlayerLogic();
		StateMachine.Set(new PlayerLogic.Data());
		StateMachine.Set(this);
		StateMachine.Set(Settings);
		StateMachine.Set(GameRepo);

		Binding = StateMachine.Bind();
		Binding.Handle((in PlayerLogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		Binding.Handle((in PlayerLogic.Output.RotationComputed output) => GlobalRotation = output.GlobalRotation);
		Binding.Handle((in PlayerLogic.Output.ToggleBobEffect output) =>
		{
			if (output.IsBobbing) DroneRepo.InvokeHoverBobStarted();
			else DroneRepo.InvokeHoverBobStopped();
		});
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

	private static void ToggleMouseCapture() => Input.SetMouseMode(IsMouseCaptured() 
		? Input.MouseModeEnum.Visible 
		: Input.MouseModeEnum.Captured);
	
	private static bool IsMouseCaptured() => Input.MouseMode == Input.MouseModeEnum.Captured;
	
	// TODO: Encapsulate the movement tilt effect in its own class TiltComponent.
	// TODO: Create a data class TiltSettings to hold configurations used by this component.

	private void OnMoveDirectionTilt(Vector2 inputDirection, double delta)
	{
		if (inputDirection.IsEqualApprox(Vector2.Zero) && DroneModel.Rotation.IsEqualApprox(Vector3.Zero))
			return;

		var targetRotation = new Vector3
		{
			X = Mathf.DegToRad(inputDirection.Y * Settings.MaxTiltAngleDegrees),
			Y = 0f,
			Z = Mathf.DegToRad(-inputDirection.X * Settings.MaxTiltAngleDegrees)
		};

		DroneModel.Rotation = DroneModel.Rotation.MoveToward(targetRotation, Settings.TiltLerpSpeed * (float) delta);
	}
	
}
