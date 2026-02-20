namespace RescueDrone;

using Godot;

public partial class SmallDroneDeliveryArea : SmallDroneReactArea
{
    [Export] private float TimeToDeliver { get; set; } = 1.5f;

    public override void _Ready()
    {
        base._Ready();
        TimeToAction = TimeToDeliver;
        DebugColor = Colors.Aqua;
    }

    protected override void OnCountdownTimeout()
    {
        var followers = DroneFormation.GetFollowers();
        foreach (var follower in followers)
        {
            DroneFormation.RemoveDrone(follower);
            EventRepository.Instance.InvokePlayerDeliveredSmallDrone();
            follower.QueueFree();
        }
    }
    
}