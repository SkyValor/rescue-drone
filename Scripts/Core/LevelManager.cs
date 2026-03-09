namespace RescueDrone;

using Godot;
using PhantomCamera;

public partial class LevelManager : Node3D
{
    [Export] private PackedScene PlayerScene { get; set; }
    [Export] private PackedScene PlayerLookAtTargetScene { get; set; }
    [Export] private PackedScene EnergyPickupScene { get; set; }
    [Export] private int EnergyPickupValue { get; set; }
    [Export] private Vector3 PhantomCameraToPlayerRelativeOffset { get; set; }

    private Drone playerDrone;
    private ProgressBar playerEnergyGauge;
    
    public override void _Ready()
    {
        InstantiatePlayer();
        var cameraTarget = SetCameraTarget();
        SetPhantomCameraToPlayer(cameraTarget);
        BootstrapEnergyGauge();
        InstantiatePickupOnRandomSpot();
    }
    
    public override void _ExitTree()
    {
        if (playerDrone?.Energy != null)
            playerDrone.Energy.EnergyChanged -= OnEnergyChanged;
    }
    
    private void InstantiatePlayer()
    {
        playerDrone = PlayerScene.Instantiate<Drone>();
        playerDrone.AddToGroup(Constants.GROUP_NAME_PLAYER);
        playerDrone.Visible = false;
        AddChild(playerDrone);
        
        var spawnPoint = GetNode<Node3D>("%PlayerSpawnPoint");
        var directionPoint = GetNode<Node3D>("%PlayerSpawnDirectionPoint");
        playerDrone.GlobalPosition = spawnPoint.GlobalPosition;
        playerDrone.LookAt(directionPoint.GlobalPosition);
        playerDrone.Visible = true;
        
        // TODO: Should we destroy spawnPoint and directionPoint or let them stay to reinstantiate player at a later time?
    }
    
    private DroneCameraLookAtTarget SetCameraTarget()
    {
        var playerCameraTarget = PlayerLookAtTargetScene.Instantiate<DroneCameraLookAtTarget>();
        playerCameraTarget.SetDrone(playerDrone);
        AddChild(playerCameraTarget);
        return playerCameraTarget;
    }
    
    private void SetPhantomCameraToPlayer(DroneCameraLookAtTarget playerCameraTarget)
    {
        var playerRotation = playerDrone.GlobalTransform.Basis.GetEuler();
        var cameraPosition = PhantomCameraToPlayerRelativeOffset.Rotated(Vector3.Up, playerRotation.Y);
        var phantomCameraNode = new Node3D();
        AddChild(phantomCameraNode);
        phantomCameraNode.GlobalPosition = cameraPosition;
        
        var phantomCamera = new PhantomCamera3D(phantomCameraNode);
        phantomCamera.FollowMode = FollowMode3D.None;
        phantomCamera.LookAtMode = LookAtMode.Simple;


        // var phantomCameraNode = GetNode<Node3D>("%PlayerPhantomCamera3D");
        // var playerBackDir = Vector3.Back.Rotated(Vector3.Up, playerRotation.Y).Normalized();
        // var cameraPosition = playerDrone.GlobalPosition + playerBackDir * 2f + Vector3.Up;
        // phantomCameraNode.GlobalPosition = cameraPosition;
        //
        // var followNode = phantomCameraNode.FindChild("DroneMovementPhantomCameraReact") as DronePhantomCameraFollow;
        // followNode?.SetDrone(playerDrone);
        //
        // var phantomCamera = phantomCameraNode.AsPhantomCamera3D();
        // phantomCamera.LookAtTarget = playerCameraTarget;
    }
    
    private void BootstrapEnergyGauge()
    {
        playerEnergyGauge = GetNode<ProgressBar>("%PlayerEnergyGauge");
        var playerEnergy = playerDrone.Energy;
        if (playerEnergy is null) 
            return;
        
        playerEnergyGauge.MaxValue = playerEnergy.MaxEnergy;
        playerEnergyGauge.Value = playerEnergy.CurrentEnergy;
        playerEnergy.EnergyChanged += OnEnergyChanged;
        playerEnergy.StartPassiveEnergyConsumption();
    }
    
    private void OnEnergyChanged(ushort currentEnergy, ushort maxEnergy)
    {
        playerEnergyGauge.Value = currentEnergy;
        if (currentEnergy == 0)
        {
            // TODO: When the energy reaches zero, end the game (GameOver)
            GD.Print("GAME OVER - Player is out of energy.");
        }
    }
    
    private void InstantiatePickupOnRandomSpot()
    {
        var pickupSpots = GetNode<Node>("%PickupSpots");
        var randomIndex = GD.RandRange(0, pickupSpots.GetChildCount() - 1); // TODO: Check that spots is not empty
        var spot = pickupSpots.GetChild<Node3D>(randomIndex);
        var pickup = EnergyPickupScene.Instantiate<EnergyPickup>();
        if (pickup is null) 
            return;
        
        pickup.Visible = false;
        AddChild(pickup);
        pickup.GlobalPosition = spot.GlobalPosition;
        pickup.SetEnergyAmount((ushort) EnergyPickupValue);
        pickup.PlayIdleAnimation();
        pickup.Visible = true;
    }
    
}
