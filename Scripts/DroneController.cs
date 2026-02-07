namespace RescueDrone;

using System;
using Godot;

public partial class DroneController : Node
{
    public event Action<float> PropulsionInput;
    public event Action<float> StrafingInput;
    public event Action<float> TurnInput;
    public event Action<float> AscensionInput;
    
    public virtual void Tick(double delta) { }
    
    protected void OnPropulsionInput(float input) => PropulsionInput?.Invoke(input);
    
    protected void OnStrafingInput(float input) => StrafingInput?.Invoke(input);
    
    protected void OnTurnInput(float input) => TurnInput?.Invoke(input);
    
    protected void OnAscensionInput(float input) => AscensionInput?.Invoke(input);
    
}
