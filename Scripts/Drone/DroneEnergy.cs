namespace RescueDrone;

using System;
using System.Collections.Generic;
using Godot;
using MEC;

public partial class DroneEnergy : Node
{
    public event Action<ushort, ushort> EnergyChanged; 
    
    [Export] private int EnergyValue { get; set; }
    [Export] private int PassiveEnergyConsumption { get; set; }
    [Export] private float TickRate { get; set; }
    
    public ushort CurrentEnergy { get; private set; }
    public ushort MaxEnergy { get; private set; }

    private float currentTickTime;
    private CoroutineHandle? energyConsumptionCoroutine;

    public override void _Ready()
    {
        if (EnergyValue < 0)
            throw new InvalidOperationException("Energy value must be greater than or equal to 0.");
        
        CurrentEnergy = (ushort) EnergyValue;
        MaxEnergy = CurrentEnergy;
    }

    public void StartPassiveEnergyConsumption()
    {
        if (energyConsumptionCoroutine is null)
        {
            energyConsumptionCoroutine = Timing.RunCoroutine(PassiveEnergyConsumptionCoroutine());
            return;
        }

        Timing.ResumeCoroutines((CoroutineHandle) energyConsumptionCoroutine);
    }

    public void PausePassiveEnergyConsumption()
    {
        if (energyConsumptionCoroutine is null)
            return;

        Timing.PauseCoroutines((CoroutineHandle) energyConsumptionCoroutine);
    }

    public void DepleteEnergy(ushort amount)
    {
        var before = CurrentEnergy;
        CurrentEnergy = (ushort) Mathf.Max(0, CurrentEnergy - amount);
        if (CurrentEnergy != before)
            EnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    public void RestoreEnergy(ushort amount)
    {
        var before = CurrentEnergy;
        CurrentEnergy = (ushort) Mathf.Min(MaxEnergy, CurrentEnergy + amount);
        if (CurrentEnergy != before)
            EnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    private IEnumerator<double> PassiveEnergyConsumptionCoroutine()
    {
        while (CurrentEnergy != 0)
        {
            yield return Timing.WaitForOneFrame;
            
            currentTickTime += (float) Timing.DeltaTime;
            if (currentTickTime < TickRate)
                continue;
            
            currentTickTime -= TickRate;
            CurrentEnergy = (ushort) Mathf.Max(0, CurrentEnergy - PassiveEnergyConsumption);
            EnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        }
    }
    
}
