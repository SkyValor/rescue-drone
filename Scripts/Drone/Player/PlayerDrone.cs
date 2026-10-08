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
	[Node] private InputComponent InputComponent { get; set; }
	[Node] private Node3D DroneModel { get; set; }
	
	public PlayerLogic StateMachine { get; private set; }
	private PlayerLogic.IBinding Binding { get; set; }

	private DroneRepo DroneRepo { get; set; }
	IDroneRepo IProvide<IDroneRepo>.Value() => DroneRepo;
	
	public void OnResolved()
	{
		DroneRepo = new DroneRepo();
		DroneRepo.SetFlyingDrone(this);
		DroneRepo.SetDroneModel(DroneModel);
		DroneRepo.SetInputComponent(InputComponent);
		this.Provide();
		
		StateMachine = new PlayerLogic();
		StateMachine.Set(new PlayerLogic.Data());
		StateMachine.Set(this);
		StateMachine.Set(Settings);
		StateMachine.Set(GameRepo);
		StateMachine.Set(InputComponent);

		Binding = StateMachine.Bind();
		Binding.Handle((in PlayerLogic.Output.VelocityComputed output) => Velocity = output.Velocity);
		Binding.Handle((in PlayerLogic.Output.RotationComputed output) => GlobalRotation = output.GlobalRotation);
		Binding.Handle((in PlayerLogic.Output.ToggleMouseCapture _) => ToggleMouseCapture());

		Binding.When((PlayerLogic.State.Idle _) => DroneRepo.InvokeDroneStoppedMoving());
		Binding.When((PlayerLogic.State.Moving _) => DroneRepo.InvokeDroneStartedMoving());
		
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
	
}
