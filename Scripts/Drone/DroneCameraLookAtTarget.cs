namespace RescueDrone;

using System.Collections.Generic;
using MEC;

using Godot;

/// <summary>
/// This object will react to a <see cref="Drone"/>'s movement input events being invoked
/// and will move accordingly. A main offset is set to ensure this target is always at that position
/// relative to the drone. Other offsets are also available to be added as extra distance in case of
/// specific drone movement.
/// </summary>
public partial class DroneCameraLookAtTarget : Node3D
{
	[Export] private Drone Drone { get; set; }
	[Export] private Vector3 MainOffset { get; set; }
	[Export] private Vector3 OnYawOffsetAdditive { get; set; }
	[Export] private Vector3 OnThrottleOffsetAdditive { get; set; }

	private Vector3 additiveOffset;
	private float yawInput;
	private float throttleInput;
	private CoroutineHandle playerReactionCoroutine;

	public override void _Ready()
	{
		if (Drone is null) 
			return;
		
		StartDroneReaction();
	}

	public override void _ExitTree()
	{
		if (Drone is null)
			return;
		
		EndDroneReaction();
	}

	public void SetDrone(Drone drone)
	{
		if (Drone is not null)
			EndDroneReaction();

		Drone = drone;
		if (Drone is not null) 
			StartDroneReaction();
	}
	
	private void StartDroneReaction()
	{
		Drone.Controller.YawInput += OnYawInput;
		Drone.Controller.ThrottleInput += OnThrottleInput;
		playerReactionCoroutine = Timing.RunCoroutine(SetPositionRelativeToDrone().CancelWith(this), Segment.PhysicsProcess);
	}

	private void EndDroneReaction()
	{
		Drone.Controller.YawInput -= OnYawInput;
		Drone.Controller.ThrottleInput -= OnThrottleInput;
		if (playerReactionCoroutine.IsValid)
			Timing.KillCoroutines(playerReactionCoroutine);
	}
	
	private void OnYawInput(float input) => yawInput = input;
	private void OnThrottleInput(float input) => throttleInput = input;

	private IEnumerator<double> SetPositionRelativeToDrone()
	{
		while (Drone is not null)
		{
			yield return Timing.WaitForOneFrame;
			
			var droneRotation = Drone.GlobalTransform.Basis.GetEuler();
			var mainOffsetRelative = MainOffset.Rotated(Vector3.Up, droneRotation.Y);
		
			var onYawOffset = OnYawOffsetAdditive * yawInput;
			var onYawOffsetRelative = onYawOffset.Rotated(Vector3.Up, droneRotation.Y);
		
			var onThrottleOffset = OnThrottleOffsetAdditive * throttleInput;
			var onThrottleOffsetRelative = onThrottleOffset.Rotated(Vector3.Up, droneRotation.Y);
		
			GlobalPosition = Drone.GlobalPosition + mainOffsetRelative + onYawOffsetRelative + onThrottleOffsetRelative;
		}
	}
	
}
