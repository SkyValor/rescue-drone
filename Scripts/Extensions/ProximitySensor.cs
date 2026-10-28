namespace RescueDrone;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoOn))]
public partial class ProximitySensor : Area3D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<IFlyingDrone> DroneInProximity;
    public event Action<IFlyingDrone> DroneOutProximity;

    public Set<IFlyingDrone> DronesInProximity { get; private set; } = [];
    
    // public IAutoSet<IFlyingDrone> VicinityDrones => vicinityDrones;
    // private readonly AutoSet<IFlyingDrone> vicinityDrones = new();

    public void OnReady()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void OnExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
        
        // vicinityDrones.Dispose();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not IFlyingDrone drone) return;
        
        var added = DronesInProximity.Add(drone);
        if (added) DroneInProximity?.Invoke(drone);
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not IFlyingDrone drone) return;
        
        var removed = DronesInProximity.Remove(drone);
        if (removed) DroneOutProximity?.Invoke(drone);
    }
    
}
