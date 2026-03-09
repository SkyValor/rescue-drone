namespace RescueDrone;

using Godot;

public partial class EnergyPickup : Area3D
{
    private AnimationPlayer animationPlayer;
    private ushort energyAmount;

    public override void _Ready()
    {
        AddToGroup(Constants.GROUP_NAME_PICKUP);
        animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        BodyEntered += OnBodyEntered;
        
        GD.Print("Energy Pickup Ready: " + Name);
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void PlayIdleAnimation() => animationPlayer.Play("idle");
    
    public void SetEnergyAmount(ushort amount) => energyAmount = amount;

    private void OnBodyEntered(Node3D other)
    {
        if (!other.IsInGroup(Constants.GROUP_NAME_PLAYER))
            return;

        if (other is not Drone playerDrone)
            return;
        
        playerDrone.Energy.RestoreEnergy(energyAmount);
        QueueFree();
        GD.Print("QueueFree: " + Name);
    }
    
}
