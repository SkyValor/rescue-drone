namespace RescueDrone;

using Godot;

public partial class DeliverySystem : Node
{
    private ushort points;

    public override void _Ready()
    {
        CallDeferred(MethodName.SetupDeliverySystem);
    }

    public override void _ExitTree()
    {
        if (EventRepository.Instance is not null)
            EventRepository.Instance.PlayerDeliveredSmallDrone -= OnDeliverySuccessful;
    }

    public ushort GetPoints() => points;

    private void SetupDeliverySystem()
    {
        EventRepository.Instance.PlayerDeliveredSmallDrone += OnDeliverySuccessful;
    }

    private void OnDeliverySuccessful()
    {
        points++;
    }
    
}