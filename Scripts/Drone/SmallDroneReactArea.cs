namespace RescueDrone;

using Godot;

public abstract partial class SmallDroneReactArea : Area3D
{
    protected DroneFormation DroneFormation;
    protected float TimeToAction;
    protected Color DebugColor;
    
    private Timer countdownToAction;
    private float areaRadius;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        var collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        if (collisionShape?.Shape is SphereShape3D sphere) 
            areaRadius = sphere.Radius;
    }
    
    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;

        if (countdownToAction is not null)
            countdownToAction.Timeout -= OnCountdownTimeout;
    }
    
    public override void _Process(double delta)
    {
        DebugDraw3D.DrawSphere(GlobalPosition, areaRadius, DebugColor);
    }
    
    private void OnBodyEntered(Node3D other)
    {
        if (other is not Drone player)
            return;

        DroneFormation = player.DroneFormation;
        StartCountdown();
    }

    private void OnBodyExited(Node3D other)
    {
        if (other is not Drone player || DroneFormation != player.DroneFormation)
            return;

        DroneFormation = null;
        StopCountdown();
    }
    
    private void StartCountdown()
    {
        if (countdownToAction is null)
        {
            countdownToAction = new Timer();
            AddChild(countdownToAction);
            countdownToAction.Timeout += OnCountdownTimeout;
        }
        else
        {
            countdownToAction.Stop();
        }
		
        countdownToAction.Start(TimeToAction);
    }
    
    private void StopCountdown()
    {
        countdownToAction?.Stop();
    }
    
    protected virtual void OnCountdownTimeout()
    {
        // droneFormation.AddDrone(SmallDrone);
        // QueueFree();
    }
    
}