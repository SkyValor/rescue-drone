namespace RescueDrone;

using Godot;

public interface IFlyingDrone;

public abstract partial class FlyingDrone : CharacterBody3D, IFlyingDrone;
