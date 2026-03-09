namespace RescueDrone;

using Godot;

public partial class Game : Node
{
    [Export] private PackedScene Level1Scene { get; set; }

    private Node3D levelContainer;
    private GameState gameState;

    public override void _Ready()
    {
        levelContainer = GetNode<Node3D>("%LevelContainer");
        
        var level = Level1Scene.Instantiate<Node3D>();
        levelContainer.AddChild(level);
        gameState = GameState.Playing;
    }
    
}
