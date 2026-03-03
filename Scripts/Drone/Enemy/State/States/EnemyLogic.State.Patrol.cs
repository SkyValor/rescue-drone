namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Godot;
using Godot.Collections;

public partial class EnemyLogic
{
	public partial record State
	{
		[Meta]
		public partial record Patrol : State, IGet<Input.PhysicsTick>
		{
			//private readonly Array<Waypoint> waypoints;
			private Waypoint currentWaypoint;
			private Waypoint previousWaypoint;

			public Patrol()
			{
				OnAttach(() => GD.Print("OnAttach Patrol"));
				OnDetach(() => GD.Print("OnDetach Patrol"));
			}

			public Transition On(in Input.PhysicsTick input)
			{
				var drone = Get<IEnemyDrone>();
				var settings = Get<Settings>();
				var velocity = drone.Velocity;
				
				currentWaypoint ??= GetClosestWaypoint();
				if (currentWaypoint is null)
				{
					AddError(new MissingFieldException("Enemy drone state machine cannot get closest waypoint."));
					return ToSelf();
				}
		
				var distanceToWaypoint = drone.GlobalPosition.DistanceTo(currentWaypoint.GlobalPosition);
				if (distanceToWaypoint < 0.25f)
				{
					var nextWaypoint = GetNextWaypoint();
					previousWaypoint = currentWaypoint;
					currentWaypoint = nextWaypoint;
				}
		
				var targetPosition = currentWaypoint.GlobalPosition;
				var direction = targetPosition - drone.GlobalPosition;

				var springForce = direction * settings.SpringStrength;
				var dampingForce = -velocity * settings.Damping;
				//var avoidanceForce = GetAvoidanceForce();
				var acceleration = springForce + dampingForce;
				velocity += acceleration * (float) input.DeltaTime;
		
				// Clamp speed
				if (velocity.Length() > settings.MaxSpeed)
					velocity = velocity.Normalized() * settings.MaxSpeed;

				Output(new Output.VelocityChanged(velocity));
				return ToSelf();
			}

			private Waypoint GetClosestWaypoint()
			{
				var drone = Get<IEnemyDrone>();
				var waypoints = Get<Array<Waypoint>>();
				Waypoint closestWaypoint = null;
				var distanceToWaypoint = float.MaxValue;
				foreach (var waypoint in waypoints)
				{
					var distance = drone.GlobalPosition.DistanceTo(waypoint.GlobalPosition);
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
