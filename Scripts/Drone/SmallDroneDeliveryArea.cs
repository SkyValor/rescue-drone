namespace RescueDrone;

using Godot;

public partial class SmallDroneDeliveryArea : Area3D
{
    [Export] private float TimeToDeliver { get; set; } = 1.5f;

    private DroneFormation droneFormation;
    private Timer countdownToDeliver;
    private float elapsedTime;
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
        
        if (countdownToDeliver is not null)
            countdownToDeliver.Timeout -= OnCountdownTimeout;
    }

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawSphere(GlobalPosition, areaRadius, Colors.Aqua);
    }

    private void OnBodyEntered(Node3D other)
    {
        if (other is not Drone player)
            return;

        droneFormation = player.DroneFormation;
        StartCountdown();
    }

    private void OnBodyExited(Node3D other)
    {
        if (other is not Drone player || droneFormation != player.DroneFormation)
            return;
        
        droneFormation = null;
        StopCountdown();
    }

    private void StartCountdown()
    {
        if (countdownToDeliver is null)
        {
            countdownToDeliver = new Timer();
            AddChild(countdownToDeliver);
            countdownToDeliver.Timeout += OnCountdownTimeout;
        }
        else
        {
            countdownToDeliver.Stop();
        }
        
        countdownToDeliver.Start(TimeToDeliver);
    }

    private void StopCountdown()
    {
        countdownToDeliver?.Stop();
    }

    private void OnCountdownTimeout()
    {
        var followers = droneFormation.GetFollowers();
        foreach (var follower in followers)
        {
            droneFormation.RemoveDrone(follower);
            EventRepository.Instance.InvokePlayerDeliveredSmallDrone();
            follower.QueueFree();
        }
    }
    
}