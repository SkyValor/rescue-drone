namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class TiltByInputComponent : Node
{
    public override void _Notification(int what) => this.Notify(what);

    [Export(PropertyHint.ResourceType, "TiltByInputSettings")]
    public TiltByInputSettings Settings { get; private set; }

    [Dependency] private IDroneRepo DroneRepo => this.DependOn<IDroneRepo>();

    private InputComponent inputComponent;
    private Vector2 inputDirection = Vector2.Zero;
    private Node3D droneModel;

    
    public void OnResolved()
    {
        inputComponent = DroneRepo.InputComponent.Value;
        droneModel = DroneRepo.DroneModel.Value;

        SetPhysicsProcess(true);
    }

    public void OnExitTree()
    {
        SetPhysicsProcess(false);
    }

    public void OnPhysicsProcess(double delta)
    {
        inputDirection = inputComponent.GetDirectionalInput();
        if (inputDirection.IsEqualApprox(Vector2.Zero) && droneModel.Rotation.IsEqualApprox(Vector3.Zero)) 
            return;

        var targetRotation = new Vector3
        {
            X = Mathf.DegToRad(inputDirection.Y * Settings.MaxTiltAngleDegrees),
            Y = 0f,
            Z = Mathf.DegToRad(-inputDirection.X * Settings.MaxTiltAngleDegrees)
        };
        
        droneModel.Rotation = droneModel.Rotation.MoveToward(targetRotation, Settings.TiltLerpSpeed * (float) delta);
    }

    private void OnInputReceived(Vector2 inputDirection) => this.inputDirection = inputDirection;
    
}
