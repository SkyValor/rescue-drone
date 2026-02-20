namespace RescueDrone;

using Godot;
using PhantomCamera;

/// <summary>
/// The class is responsible for listening for any events related to the player rescuing NPC drones and updating
/// the <see cref="PhantomCamera3D"/>'s FOV value.
/// </summary>
public partial class PlayerPhantomCameraOnRescueChanged : Node
{
	[Export] private float FovWhenAlone { get; set; } = 75f;
	[Export] private float FovWhenFollowed { get; set; } = 100f;
	[Export] private float TweenTime { get; set; } = 0.75f;
	
	private Camera3DResource camera3DResource;
	private Tween fovAnimationTween;
	private float targetFov;
	
	public override void _Ready()
	{
		EventRepository.Instance.PlayerSmallDronesFollowing += OnFollowersChanged;
		camera3DResource = GetParentOrNull<Node3D>().AsPhantomCamera3D().Camera3DResource;
	}

	public override void _ExitTree()
	{
		EventRepository.Instance.PlayerSmallDronesFollowing -= OnFollowersChanged;
	}

	private void OnFollowersChanged(ushort followers)
	{
		var fov = followers == 0 ? FovWhenAlone : FovWhenFollowed;
		if (Mathf.IsEqualApprox(targetFov, fov))
			return;
		
		var currentFov = camera3DResource.Fov;
		targetFov = fov;
		
		if (fovAnimationTween != null && fovAnimationTween.IsRunning())
			fovAnimationTween.Kill();
		
		fovAnimationTween = CreateTween();
		fovAnimationTween.SetEase(Tween.EaseType.Out);
		fovAnimationTween.SetTrans(Tween.TransitionType.Sine);
		fovAnimationTween.TweenMethod(
			method: Callable.From((float fovValue) => camera3DResource.Fov = fovValue),
			from: currentFov,
			to: targetFov,
			duration: TweenTime);
		fovAnimationTween.Play();
	}
	
}
