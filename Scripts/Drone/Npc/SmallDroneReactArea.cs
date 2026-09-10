namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public abstract partial class SmallDroneReactArea : Area3D
{
    public override void _Notification(int what) => this.Notify(what);

    [Dependency] protected IGameRepo GameRepo => this.DependOn<IGameRepo>();
    
    protected DroneFormation DroneFormation;
    protected float TimeToAction;
    protected Color DebugColor;
    
    private Timer countdownToAction;
    private float areaRadius;

    public virtual void OnReady()
    {
        SetProcess(true);
        var collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        if (collisionShape?.Shape is SphereShape3D sphere) 
            areaRadius = sphere.Radius;
    }

    public void OnEnterTree()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void OnExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;

        if (countdownToAction is not null)
            countdownToAction.Timeout -= OnCountdownTimeout;
    }

    public void OnProcess(double delta)
    {
        DrawReactionArea();
    }
    
    private void OnBodyEntered(Node3D other)
    {
        if (other is not PlayerDrone player)
            return;

        DroneFormation = player.Formation;
        StartCountdown();
    }

    private void OnBodyExited(Node3D other)
    {
        if (other is not PlayerDrone player || DroneFormation != player.Formation)
            return;

        DroneFormation = null;
        StopCountdown();
    }
    
    private void StartCountdown()
    {
        if (countdownToAction is null)
        {
            countdownToAction = new Timer();
            countdownToAction.OneShot = true;
            countdownToAction.WaitTime = TimeToAction;
            countdownToAction.Timeout += OnCountdownTimeout;
            AddChild(countdownToAction);
            
        }
        else
        {
            countdownToAction.Stop();
        }
		
        countdownToAction.Start();
    }
    
    private void StopCountdown()
    {
        countdownToAction?.Stop();
    }

    private void DrawReactionArea() => DebugDraw3D.DrawSphere(GlobalPosition, areaRadius, DebugColor);
    
    protected virtual void OnCountdownTimeout() { }
    
}
