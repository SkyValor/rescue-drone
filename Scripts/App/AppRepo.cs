namespace RescueDrone;

using System;

public interface IAppRepo : IDisposable
{
    event Action GameEntered;
    event Action GameExited;
    event Action MainMenuEntered;

    void OnEnterGame();
    void OnExitGame();
    void OnMainMenuEnter();
}

public class AppRepo : IAppRepo
{
    public event Action GameEntered;
    public event Action GameExited;
    public event Action MainMenuEntered;

    private bool disposedValue;
    
    public void OnEnterGame() => GameEntered?.Invoke();
    public void OnExitGame() => GameExited?.Invoke();
    public void OnMainMenuEnter() => MainMenuEntered?.Invoke();

    #region Internals
    private void Dispose(bool disposing)
    {
        if (disposedValue) return;
        if (disposing)
        {
            // Dispose managed objects.
            GameEntered = null;
            GameExited = null;
            MainMenuEntered = null;
        }

        disposedValue = true;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
    
}
