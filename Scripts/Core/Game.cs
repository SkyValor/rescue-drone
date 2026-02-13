namespace RescueDrone.Scripts.Core;

using System.Collections.Generic;
using Godot;
using MEC;

public partial class Game : Node
{
    [Export] private PackedScene PlayerScene { get; set; }
    
    
    
    [Export] private float timeBeforeStart { get; set; }

    private Drone playerDrone;

    private GameState gameState;
    private CoroutineHandle? waitTimer;

    public override void _Ready()
    {
        playerDrone = PlayerScene.Instantiate<Drone>();
        AddChild(playerDrone);
        var spawnPoint = GetNode<Node3D>("%PlayerSpawnPoint");
        playerDrone.GlobalPosition = spawnPoint.GlobalPosition;
        waitTimer = Timing.RunCoroutine(WaitAndStart());
    }

    private IEnumerator<double> WaitAndStart()
    {
        yield return Timing.WaitForSeconds(timeBeforeStart);
        GD.Print("Game started!");
    }
    
}
