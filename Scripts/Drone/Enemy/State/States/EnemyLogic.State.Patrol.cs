namespace RescueDrone;

using System;
using Chickensoft.LogicBlocks;
using Godot;
using Godot.Collections;

public partial class EnemyLogic
{
	public partial record State
	{
		public record Patrol : State, IGet<Input.PhysicsTick>
		{
			private Waypoint currentWaypoint;
			private Waypoint previousWaypoint;

			public Patrol()
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

			public Transition On(in Input.PhysicsTick input)
			{
				var player = Get<Drone>();
				var enemy = Get<EnemyDrone>();
				var settings = Get<Settings>();
				
				if (PlayerIsInLineOfSight(enemy, player, settings))
				{
					Get<Data>().LastPlayerKnownPosition = player.GlobalPosition;
					return To<Attack>();
				}
				
				var distanceToWaypoint = enemy.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
				if (distanceToWaypoint < 0.05f)
				{
					return To<Lookout>().With(SetupLookoutState(settings));
				}

				Output(new Output.MoveTowards(currentWaypoint.GlobalPosition, input.DeltaTime));
				return ToSelf();
			}

			private Action<State> SetupLookoutState(Settings settings)
			{
				return state =>
				{
					var lookoutState = (Lookout) state;
					lookoutState.LookoutAngle = settings.PatrolLookoutAngle;
					lookoutState.LookoutRotationTime = settings.PatrolLookoutRotationTime;
					lookoutState.LookoutHoldDuration = settings.PatrolLookoutHoldDuration;
					lookoutState.OnLookoutFinishedAction = () =>
					{
						var nextWaypoint = GetNextWaypoint();
						previousWaypoint = currentWaypoint;
						currentWaypoint = nextWaypoint;
					};
					lookoutState.OnLookoutFinishedNextState = typeof(Patrol);
				};
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
