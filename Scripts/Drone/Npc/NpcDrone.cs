using Godot;

namespace RescueDrone.Npc;

public partial class NpcDrone : Drone
{
    [ExportGroup("Drone Movement Stats")]
    [Export] private float SpringStrength { get; set; } = 12f;
    [Export] private float Damping { get; set; } = 8f;
    [Export] private float MaxSpeed { get; set; } = 10f;
    
    [Export] private float OscillationMagnitude { get; set; } = 0.05f;
    [Export] private float OscillationHeight { get; set; } = 0.5f;

    [Export] private float AvoidanceStrength { get; set; } = 20f;
    [Export] private float AvoidanceDistance { get; set; } = 4f;
}