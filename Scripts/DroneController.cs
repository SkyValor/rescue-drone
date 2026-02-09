namespace RescueDrone;

using System;
using Godot;

public partial class DroneController : Node
{
    public event Action<float> PropulsionInput;
    public event Action<float> StrafingInput;
    public event Action<float> TurnInput;
    public event Action<float> ThrottleInput;
    
    public virtual void Tick() { }
    
    protected void OnPropulsionInput(float input) => PropulsionInput?.Invoke(input);
    
    protected void OnStrafingInput(float input) => StrafingInput?.Invoke(input);
    
    protected void OnTurnInput(float input) => TurnInput?.Invoke(input);
    
    protected void OnThrottleInput(float input) => ThrottleInput?.Invoke(input);
    
}
