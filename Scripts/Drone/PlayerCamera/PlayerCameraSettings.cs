namespace RescueDrone;

using Godot;

[GlobalClass]
public partial class PlayerCameraSettings : Resource
{
    [Export] public float MouseSensitivity { get; set; } = 0.005f;

    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")] 
    public float LerpPower { get; private set; } = 5f;
    
    [Export(PropertyHint.Range, "-90.0, 0.0, 0.1")]
    public float MinVerticalAngle { get; private set; } = -90f;

    [Export(PropertyHint.Range, "0.0, 90.0, 0.1")]
    public float MaxVerticalAngle { get; private set; } = 45f;

    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MinZoom { get; private set; } = 2f;
    
    [Export(PropertyHint.Range, "1.0, 20.0, 0.1")]
    public float MaxZoom { get; private set; } = 12f;
}
