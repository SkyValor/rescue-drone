namespace RescueDrone;

using System;
using Godot;

public partial class DroneController : Node
{
    public event Action<float> PitchInput;
    public event Action<float> RollInput;
    public event Action<float> YawInput;
    public event Action<float> ThrottleInput;
    
    public virtual void Tick() { }
    
    protected void OnPitchInput(float input) => PitchInput?.Invoke(input);
    
    protected void OnRollInput(float input) => RollInput?.Invoke(input);
    
    protected void OnYawInput(float input) => YawInput?.Invoke(input);
    
    protected void OnThrottleInput(float input) => ThrottleInput?.Invoke(input);
    
}
