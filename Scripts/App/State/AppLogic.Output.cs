namespace RescueDrone;

public partial class AppLogic
{
    public static class Output
    {
        public readonly record struct ShowMainMenu;
        public readonly record struct FadeToBlack;
        public readonly record struct TransitionToBlack;
        public readonly record struct TransitionToNormal;
        public readonly record struct SetupDroneGame;
        public readonly record struct ShowGame;
        public readonly record struct HideGame;
        
    }
}
