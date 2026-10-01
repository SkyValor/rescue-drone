namespace RescueDrone;

public enum InputType
{ 
    KeyboardAndMouse,
    KeyboardOnly,
    Joypad,
}

public static class InputTypeExtensions
{
    public static bool IsKeyboardInclusive(this InputType inputType) =>
        inputType is InputType.KeyboardAndMouse or InputType.KeyboardOnly;
}
