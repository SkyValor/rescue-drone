namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IMainMenu : IControl
{
    event Action NewGame;
    event Action Options;
}

[Meta(typeof(IAutoNode))]
public partial class Menu : Control, IMainMenu
{
    public override void _Notification(int what) => this.Notify(what);
    
    public event Action NewGame;
    public event Action Options;
    
    #region Nodes
    [Node] public IButton NewGameButton { get; private set; }
    [Node] public IButton OptionsButton { get; private set; }
    #endregion

    public void OnReady()
    {
        NewGameButton.Pressed += OnNewGamePressed;
        OptionsButton.Pressed += OnOptionsPressed;
    }

    public void OnExitTree()
    {
        NewGameButton.Pressed -= OnNewGamePressed;
        OptionsButton.Pressed -= OnOptionsPressed;
    }
    
    private void OnNewGamePressed() => NewGame?.Invoke();
    private void OnOptionsPressed() => Options?.Invoke();
    
}
