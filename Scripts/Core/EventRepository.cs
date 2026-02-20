using System;

namespace RescueDrone;

using Godot;

public partial class EventRepository : Node
{
	public event Action<ushort> PlayerSmallDronesFollowing;
	public event Action PlayerDeliveredSmallDrone;
	
	public static EventRepository Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	public void InvokePlayerSmallDronesFollowing(ushort numberOfFollowers) 
		=> PlayerSmallDronesFollowing?.Invoke(numberOfFollowers);
	
	public void InvokePlayerDeliveredSmallDrone() 
		=> PlayerDeliveredSmallDrone?.Invoke();
	
}
