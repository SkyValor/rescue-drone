namespace RescueDrone.Scripts.Core;

using Godot;
using PhantomCamera;

public partial class Game : Node
{
    [Export] private PackedScene PlayerScene { get; set; }
    [Export] private PackedScene PlayerLookAtTargetScene { get; set; }

    private Drone playerDrone;
    private GameState gameState;

    public override void _Ready()
    {
        playerDrone = PlayerScene.Instantiate<Drone>();
        AddChild(playerDrone);

        // Instantiate the player drone on the spawn point
        var spawnPoint = GetNode<Node3D>("%PlayerSpawnPoint");
        if (spawnPoint is not null)
            playerDrone.GlobalPosition = spawnPoint.GlobalPosition;
        
        var droneRotation = playerDrone.GlobalTransform.Basis.GetEuler();
        
        // Set the camera target for the player's PhantomCamera
        var playerCameraTarget = PlayerLookAtTargetScene.Instantiate<DroneCameraLookAtTarget>();
        playerCameraTarget.SetDrone(playerDrone);
        AddChild(playerCameraTarget);
        var playerFrontDir = Vector3.Forward.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        var playerCameraTargetPosition = playerDrone.GlobalPosition + playerFrontDir * 24;
        playerCameraTarget.GlobalPosition = playerCameraTargetPosition;
        
        // Set the PhantomCamera to be behind the player drone.
        var phantomCameraNode = GetNode<Node3D>("%PlayerPhantomCamera3D");
        var playerBackDir = Vector3.Back.Rotated(Vector3.Up, droneRotation.Y).Normalized();
        var cameraPosition = playerDrone.GlobalPosition + playerBackDir * 2f + Vector3.Up;
        phantomCameraNode.GlobalPosition = cameraPosition;
        
        var followNode = phantomCameraNode.FindChild("DroneMovementPhantomCameraReact") as DronePhantomCameraFollow;
        followNode?.SetDrone(playerDrone);

        var phantomCamera = phantomCameraNode.AsPhantomCamera3D();
        phantomCamera.LookAtTarget = playerCameraTarget;
    }
    
}
