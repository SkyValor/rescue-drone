namespace RescueDrone;

public partial class AppLogic
{
    public static class Input
    {
        public readonly record struct NewGame;
        public readonly record struct Options;
        public readonly record struct FadeInFinished;
        public readonly record struct FadeOutFinished;
        public readonly record struct EndGame;
    }
}
