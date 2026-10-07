namespace RescueDrone;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using MEC;

[Meta(typeof(IAutoOn), typeof(IDependent))]
public partial class HoverBobComponent : Node
{
    public override void _Notification(int what) => this.Notify(what);

    private const string COROUTINE_TAG = "hover_tag";
    
    [Export(PropertyHint.ResourceType, "HoverBobSettings")] 
    public HoverBobSettings Settings { get; private set; }

    [Dependency] private IDroneRepo DroneRepo => this.DependOn<IDroneRepo>();

    private float bobbingTime;
    private bool isBobbing;
    
    public void OnResolved()
    {
        DroneRepo.DroneStoppedMoving += StartHoverBob;
        DroneRepo.DroneStartedMoving += GoBackToOrigin;
    }

    public void OnExitTree()
    {
        DroneRepo.DroneStoppedMoving -= StartHoverBob;
        DroneRepo.DroneStartedMoving -= GoBackToOrigin;
    }

    private void StartHoverBob()
    {
        if (isBobbing) return;
        
        isBobbing = true;
        Timing.KillCoroutines(COROUTINE_TAG);
        Timing.RunCoroutine(HoverBobCoroutine().CancelWith(DroneRepo.DroneModel.Value), 
            Segment.PhysicsProcess, COROUTINE_TAG);
    }
    
    private void GoBackToOrigin()
    {
        if (!isBobbing) return;
        
        isBobbing = false;
        Timing.KillCoroutines(COROUTINE_TAG);
        Timing.RunCoroutine(ReturnToOriginCoroutine().CancelWith(DroneRepo.DroneModel.Value), 
            Segment.PhysicsProcess, COROUTINE_TAG);
    }

    private IEnumerator<double> HoverBobCoroutine()
    {
        var droneModel = DroneRepo.DroneModel.Value;
        bobbingTime = 0f;
        while (isBobbing)
        {
            var meshPosition = droneModel.Position;
            bobbingTime += (float) Timing.DeltaTime;
            
            var bobOffset = Mathf.Sin(bobbingTime * Settings.Frequency) * Settings.Amplitude;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, bobOffset, 0.1f);
            droneModel.Position = meshPosition;

            yield return Timing.WaitForOneFrame;
        }
    }

    private IEnumerator<double> ReturnToOriginCoroutine()
    {
        var droneModel = DroneRepo.DroneModel.Value;
        while (droneModel.Position.IsNotZeroApprox())
        {
            // Return to local origin smoothly
            var meshPosition = droneModel.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, 0f, 0.1f);
            droneModel.Position = meshPosition;
            yield return Timing.WaitForOneFrame;
        }
    }
    
}
