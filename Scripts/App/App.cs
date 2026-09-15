namespace RescueDrone;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class App : CanvasLayer, IProvide<IAppRepo>
{
    public override void _Notification(int what) => this.Notify(what);
    
    [Export] private PackedScene GameScene { get; set; }
    
    private DroneGame Game { get; set; }
    
    #region State
    private IAppRepo AppRepo { get; set; }
    private IAppLogic AppLogic { get; set; }
    private LogicBlock<AppLogic.State>.IBinding AppBinding { get; set; }
    #endregion
    
    #region Nodes
    [Node] private IMainMenu MainMenu { get; set; }
    [Node] private IColorRect BlankScreen { get; set; }
    [Node] private IColorRect TransitionScreen { get; set; }
    [Node] private IAnimationPlayer AnimationPlayer { get; set; }
    #endregion
    
    IAppRepo IProvide<IAppRepo>.Value() => AppRepo;

    public void Initialize()
    {
        AppRepo = new AppRepo();
        AppLogic = new AppLogic();
        AppLogic.Set(AppRepo);
        AppLogic.Set(new AppLogic.Data());

        MainMenu.NewGame += OnNewGame;
        MainMenu.Options += OnOptions;
        AnimationPlayer.AnimationFinished += OnAnimationFinished;
        
        this.Provide();
    }

    public void OnReady()
    {
        AppBinding = AppLogic.Bind();
        AppBinding
            .Handle((in AppLogic.Output.ShowMainMenu _) => 
            { 
                MainMenu.Show(); 
                FadeInFromBlack(); 
            })
            .Handle((in AppLogic.Output.TransitionToBlack _) =>
            {
                
            })
            .Handle((in AppLogic.Output.SetupDroneGame _) =>
            {
                Game = GameScene.Instantiate<DroneGame>();
                AddChild(Game);
            });
        
        AppLogic.Start();
    }

    public void OnExitTree()
    {
        AppLogic.Stop();
        AppBinding.Dispose();
        AppRepo.Dispose();

        MainMenu.NewGame -= OnNewGame;
        MainMenu.Options -= OnOptions;
        AnimationPlayer.AnimationFinished -= OnAnimationFinished;
    }

    private void OnNewGame() => AppLogic.Input(new AppLogic.Input.NewGame());

    private void OnOptions()
    {
        // Hide main menu and show options menu.
        MainMenu.Hide();
    }

    private void OnAnimationFinished(StringName animation)
    {
        if (animation.Equals("fade_in"))
        {
            AppLogic.Input(new AppLogic.Input.FadeInFinished());
            BlankScreen.Hide();
            return;
        }

        AppLogic.Input(new AppLogic.Input.FadeOutFinished());
    }

    private void FadeInFromBlack()
    {
        BlankScreen.Show();
        AnimationPlayer.Play("fade_in");
    }

    private void FadeToBlack()
    {
        BlankScreen.Show();
        AnimationPlayer.Play("fade_out");
    }

    private void TransitionToBlack()
    {
        // TODO: Might have to set shader parameters here...
        AnimationPlayer.Play("transition_black");
    }
    
}
