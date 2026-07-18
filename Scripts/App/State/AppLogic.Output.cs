namespace RescueDrone;

public partial class AppLogic
{
    public static class Output
    {
        public readonly record struct ShowMainMenu;
        public readonly record struct FadeToBlack;
        public readonly record struct ShowGame;
        public readonly record struct HideGame;
        
    }
}
