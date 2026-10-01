namespace RescueDrone;

using Godot;

public static class PrimitiveExtensions
{
    public static bool IsZeroApprox(this float value) => Mathf.IsZeroApprox(value);
    
    public static bool IsNotZeroApprox(this float value) => !value.IsZeroApprox();
    
    public static bool IsEqualApprox(this float thisValue, float otherValue) => Mathf.IsEqualApprox(thisValue, otherValue);
}
