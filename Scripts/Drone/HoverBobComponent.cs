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
    
    [Dependency] private PlayerLogic PlayerLogic => this.DependOn<PlayerLogic>();
    [Dependency] private DroneModel DroneModel => this.DependOn<DroneModel>();

    private PlayerLogic.IBinding Binding { get; set; }
    private float bobbingTime;
    private bool isBobbing;
    
    public void OnResolved()
    {
        if (PlayerLogic is null) return;

        Binding = PlayerLogic.Bind();
        Binding.Handle((in PlayerLogic.Output.ToggleBobEffect output) => ToggleBobEffect(output.IsBobbing));
    }

    public void OnExitTree()
    {
        Binding?.Dispose();
    }

    private void ToggleBobEffect(bool isBobbing)
    {
        if (this.isBobbing == isBobbing) return;
        
        this.isBobbing = isBobbing;
        Timing.KillCoroutines(COROUTINE_TAG);
            
        if (isBobbing) StartHoverBob();
        else GoBackToOrigin();
    }

    private void StartHoverBob()
    {
        Timing.RunCoroutine(HoverBobCoroutine().CancelWith(DroneModel), Segment.PhysicsProcess, COROUTINE_TAG);
    }
    
    private void GoBackToOrigin()
    {
        Timing.RunCoroutine(ReturnToOriginCoroutine().CancelWith(DroneModel), Segment.PhysicsProcess, COROUTINE_TAG);
    }

    private IEnumerator<double> HoverBobCoroutine()
    {
        bobbingTime = 0f;
        while (isBobbing)
        {
            var meshPosition = DroneModel.Position;
            bobbingTime += (float) Timing.DeltaTime;
            
            var bobOffset = Mathf.Sin(bobbingTime * Settings.Frequency) * Settings.Amplitude;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, bobOffset, 0.1f);
            DroneModel.Position = meshPosition;

            yield return Timing.WaitForOneFrame;
        }
    }

    private IEnumerator<double> ReturnToOriginCoroutine()
    {
        while (DroneModel.Position.IsNotZeroApprox())
        {
            // Return to local origin smoothly
            var meshPosition = DroneModel.Position;
            meshPosition.Y = Mathf.Lerp(meshPosition.Y, 0f, 0.1f);
            DroneModel.Position = meshPosition;
            yield return Timing.WaitForOneFrame;
        }
    }
    
}
