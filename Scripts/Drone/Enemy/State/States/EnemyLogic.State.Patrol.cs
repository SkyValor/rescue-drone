namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;
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
					GD.Print("Patrol");
					currentWaypoint = GetClosestWaypoint();
				});
				
				this.OnExit(() =>
				{
					currentWaypoint = null;
					previousWaypoint = null;
				});
			}

			public virtual Transition On(in Input.PhysicsTick input)
			{
				var player = Get<Drone>();
				var enemy = Get<EnemyDrone>();
				var settings = Get<Settings>();
				
				if (PlayerIsInLineOfSight(enemy, player, settings))
				{
					GD.Print("Line of sight to player.");
					Get<Data>().LastPlayerKnownPosition = player.GlobalPosition;
					return To<Attack>();
				}
				
				var distanceToWaypoint = enemy.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
				if (distanceToWaypoint < 0.05f)
				{
					return To<Lookout>().With(state =>
					{
						var lookoutState = (Lookout) state;
						lookoutState.LookoutAngle = settings.PatrolLookoutAngle;
						lookoutState.LookoutRotationTime = settings.PatrolLookoutRotationTime;
						lookoutState.LookoutHoldDuration = settings.PatrolLookoutHoldDuration;
						lookoutState.OnLookoutFinished = () =>
						{
							var nextWaypoint = GetNextWaypoint();
							previousWaypoint = currentWaypoint;
							currentWaypoint = nextWaypoint;
							return To<Patrol>();
						};
					});
				}

				Output(new Output.MoveTowards(currentWaypoint.GlobalPosition, input.DeltaTime));
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
