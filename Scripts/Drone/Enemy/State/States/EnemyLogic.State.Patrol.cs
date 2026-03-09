namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
	public partial record State
	{
		[Meta]
		public partial record Patrol : State
		{
			private Waypoint currentWaypoint;
			private Waypoint previousWaypoint;

			public Patrol()
			{
				this.OnEnter(() =>
				{
					GD.Print("Enemy on Patrol");
					currentWaypoint = null;
					previousWaypoint = null;
				});
			}
			
		}
	}
}
