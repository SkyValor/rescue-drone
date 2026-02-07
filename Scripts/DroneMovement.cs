namespace RescueDrone;

using Godot;

public partial class DroneMovement : Node
{
    [ExportGroup("Propulsion")]
    [Export] protected Curve AccelCurve { get; set; }
    [Export] protected float MaxPropulsionSpeed { get; set; } = 200f;
    [Export] protected float AccelWeight { get; set; } = 0.5f;
    [ExportGroup("Strafing")]
    [Export] protected Curve StrafingCurve { get; set; }
    [Export] protected float MaxStrafingSpeed { get; set; } = 80f;
    [Export] protected float StrafingWeight { get; set; } = 0.5f;
    [ExportGroup("Rotation")]
    [Export] protected float RotationSpeed { get; set; } = 50f;

    protected Drone Drone;
    protected float PropulsionIntent;
    protected float CurrentPropulsionForce;
    protected float StrafingIntent;
    protected float CurrentStrafingForce;
    protected float TurningIntent;
    
    public void SetDrone(Drone drone) => Drone = drone;

    public void SetPropulsionIntent(float intent) => PropulsionIntent = intent;
    
    public void SetStrafingIntent(float intent) => StrafingIntent = intent;
    
    public void SetTurningIntent(float intent) => TurningIntent = intent;

    public void Tick(float delta)
    {
        RotateDrone(delta);
        MoveDrone(delta);
    }

    protected virtual void MoveDrone(float delta) { }
    
    protected virtual void RotateDrone(float delta) { }
    
}
