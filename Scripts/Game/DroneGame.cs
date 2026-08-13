namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using MEC;

// TODO: Check if a Waypoint requires the SVO to subdivide until reaching its position, as if it's a physical object.
// This would benefit the accuracy of pathfinding.

// TODO: Load up these in order:
// 1. The map
// 2. Sparse Voxel Octree ramifications
// 3. Waypoint circuits, Player + Player camera, Enemies

[Meta(typeof(IAutoNode))]
public partial class DroneGame : Node3D, IProvide<IGameRepo>
{
    public override void _Notification(int what) => this.Notify(what);

    #region Exports
    [Export] private Vector3 WorldCenter { get; set; } = Vector3.Zero;
    [Export] private float WorldSize { get; set; } = 50f;
    [Export] private float MinNodeSize { get; set; } = 2f;
    [Export] private float LoadingScreenFadeOutDuration { get; set; } = 3f;
    [Export] private float LoadingScreenFadeOutValue { get; set; } = 1f;
    
    [Export] private PackedScene EnemyDroneScene { get; set; }
    [Export] private PackedScene PlayerDroneScene { get; set; }
    
    [Export] private Vector3 FollowCameraOffset { get; set; }
    [Export] private Vector3 FollowCameraRotation { get; set; }
    #endregion
    
    // [Dependency] private IAppRepo AppRepo => this.DependOn<IAppRepo>();
    
    #region Nodes
    [Node] private ColorRect LoadingScreen { get; set; }
    [Node] private ProgressBar LoadingBar { get; set; }
    [Node] private Label LoadingLabel { get; set; }
    
    [Node] private SVOBuilder SVOBuilder { get; set; }
    
    [Node] private Node WaypointCircuits { get; set; }
    [Node] private Node3D PlayerSpawnPoint { get; set; }
    [Node] private Node3D EnemySpawnPoint { get; set; }
    #endregion
    
    private IGameRepo GameRepo { get; set; }
    IGameRepo IProvide<IGameRepo>.Value() => GameRepo;

    private ShaderMaterial loadingScreenShader;

    public void OnReady()
    {
        GameRepo = new GameRepo();
        loadingScreenShader = LoadingScreen.Material as ShaderMaterial;
        if (loadingScreenShader is null)
        {
            GD.PrintErr("Loading screen is missing shader material.");
        }
        else
        {
            loadingScreenShader.SetShaderParameter("progress", 0f);
        }

        LoadingBar.Visible = true;
        LoadingBar.Value = 0f;
        LoadingLabel.Visible = true;
        LoadingLabel.Text = "Generating Sparse Voxel Octree...";
        
        SVOBuilder.StartAsyncGeneration(WorldCenter, WorldSize, MinNodeSize);
        SetProcess(true);
        this.Provide();
    }

    // public void OnResolved()
    // {
    //     GameRepo = new GameRepo();
    //     SVOBuilder.StartAsyncGeneration(WorldCenter, WorldSize, MinNodeSize, GetWorld3D());
    //     this.Provide();
    // }

    public void OnProcess(double delta)
    {
        var currentProgress = SVOBuilder.Progress;
        LoadingBar.Value = currentProgress * 100f;

        if (!SVOBuilder.IsDone) return;
        
        SetProcess(false);
        GameRepo.SetSVO(SVOBuilder.Tree);
        
        // Timing.RunCoroutine(DrawVoxelOctree());
        
        OnLevelReady();
    }

    private void OnLevelReady()
    {
        // Call this method right after creating the SVO
        // but before placing player and enemies.
        
        // LoadPlayerAndEnemy();
        
        GD.Print("SVO Ready. Player can now start flying!");
        LoadingLabel.Text = "SVO ready. Player can now start flying!";

        var setup = CreateTween();
        setup.TweenInterval(2.0);
        setup.TweenCallback(Callable.From(() =>
        {
            LoadingBar.Visible = false;
            LoadingLabel.Visible = false;
        }));
        setup.TweenCallback(Callable.From(TweenLoadingScreenFadeOut));
        // LoadPlayerAndEnemy();
    }

    private IEnumerator<double> DrawVoxelOctree()
    {
        while (true)
        {
            SVOBuilder.Tree.DrawVoxelOctree();
            yield return Timing.WaitForOneFrame;
        }
    }

    private void LoadPlayerAndEnemy()
    {
        // var enemy = EnemyDroneScene.Instantiate<EnemyAIDrone>();
        // AddChild(enemy);
        // enemy.GlobalPosition = EnemySpawnPoint.GlobalPosition;

        var player = PlayerDroneScene.Instantiate<PlayerMover>();
        AddChild(player);
        player.GlobalPosition = PlayerSpawnPoint.GlobalPosition;

        var playerCamera = new Camera3D();
        player.AddChild(playerCamera);
        playerCamera.Position = FollowCameraOffset;
        playerCamera.LookAt(player.GlobalPosition);
    }

    private void TweenLoadingScreenFadeOut()
    {
        if (loadingScreenShader is null) return;
        
        var to = LoadingScreenFadeOutValue;
        var duration = LoadingScreenFadeOutDuration;
        var tween = CreateTween();
        tween.TweenProperty(loadingScreenShader, "shader_parameter/progress", to, duration);
        tween.TweenCallback(Callable.From(OnLoadingScreenFadeOutCompleted));
        tween.Play();
    }

    private static void OnLoadingScreenFadeOutCompleted()
    {
        GD.Print("Loading screen fade out animation completed. Level can start!");
    }

}
