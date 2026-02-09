namespace RescueDrone;

using Godot;

public static class PrimitiveExtensions
{
    public static bool IsZeroApprox(this float value) => Mathf.IsZeroApprox(value);
}
