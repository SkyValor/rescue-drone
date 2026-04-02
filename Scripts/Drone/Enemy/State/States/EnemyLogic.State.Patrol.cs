namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot.Collections;

public partial class EnemyLogic
{
	public partial record State
	{
		[Meta]
		public abstract partial record Patrol : State, IGet<Input.PhysicsTick>
		{
			private Waypoint currentWaypoint;
			private Waypoint previousWaypoint;

			protected Patrol()
			{
				this.OnEnter(() =>
				{
					Godot.GD.Print("Patrol");
					currentWaypoint = GetClosestWaypoint();
				});
				
				this.OnExit(() =>
				{
					currentWaypoint = null;
					previousWaypoint = null;
				});
			}

			public Transition On(in Input.PhysicsTick input)
			{
				var player = Get<Drone>();
				var enemy = Get<EnemyDrone>();
				var settings = Get<Settings>();
				if (HasLineOfSight(enemy, player, settings))
				{
					var playerPosition = player.GlobalPosition;
					return To<Attack>().With(attackState => ((Attack) attackState).LastPlayerKnownPosition = playerPosition);
				}

				return ToSelf();
			}

			private Waypoint GetClosestWaypoint()
			{
				var waypoints = Get<Array<Waypoint>>();
				var enemy = Get<EnemyDrone>();
				var distanceToWaypoint = float.MaxValue;
				Waypoint closestWaypoint = null;
				foreach (var waypoint in waypoints)
				{
					var distance = enemy.GlobalPosition.DistanceTo(waypoint.GlobalPosition);
					if (distance >= distanceToWaypoint) 
						continue;
				
					distanceToWaypoint = distance;
					closestWaypoint = waypoint;
				}
			
				return closestWaypoint;
			}

			private Waypoint GetNextWaypoint()
			{
				var connections = currentWaypoint.Connections.Duplicate();
				if (previousWaypoint is not null)
					connections.Remove(previousWaypoint);

				return connections.Count == 0 ? previousWaypoint : connections.PickRandom();
			}
			
		}
	}
}
