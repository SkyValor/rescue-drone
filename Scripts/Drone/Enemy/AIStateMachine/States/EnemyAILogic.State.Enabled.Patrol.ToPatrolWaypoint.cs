namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone attempts to get the next patrol point from the current position in the circuit.
        /// To do so, the drone generates a pathway towards it and slowly flies along it.
        ///
        /// When reaching the target patrol point, it changes the state to <see cref="State.Scanning"/>.
        /// </summary>
        [Meta]
        public partial record ToPatrolWaypoint : Patrol, IGet<Input.StartScanning>
        {
            #region Error messages
            private const string Prefix = "Error in state ToPatrolWaypoint. ";
            private const string ToIdle = " Fallback to Idle state.";
            private const string MissingCircuitInData = Prefix + "Data has no waypoint nor circuit." + ToIdle;
            private const string MissingNearestWaypoint = Prefix + "Current circuit cannot return the nearest waypoint." + ToIdle;
            private const string DistanceTooGreat = Prefix + "Distance to nearest waypoint is greater than 2.0. Sticking to that target.";
            private const string MissingConnections = Prefix + "Waypoint does not have connections." + ToIdle;
            private const string NoPathToWaypoint = Prefix + "Pathfinder did not find a path to the next waypoint." + ToIdle;
            #endregion

            private const string MovingToWaypoint = "Moving to waypoint '{0}'";

            public ToPatrolWaypoint()
            {
                this.OnEnter(() =>
                {
                    TryGettingNextWaypoint();
                    GeneratePathToWaypoint();
                });
            }
            
            private void TryGettingNextWaypoint()
            {
                var data = Get<Data>();
                if (data.CurrentWaypoint is null)
                {
                    if (data.CurrentCircuit is null)
                    {
                        DebugConsole.PrintError(MissingCircuitInData);
                        Input(new Input.ReturnToIdle());
                        return;
                    }

                    var enemyPosition = Get<EnemyAIDrone>().GlobalPosition;
                    var nearestWaypoint = data.CurrentCircuit.GetClosestWaypoint(enemyPosition);
                    if (nearestWaypoint is null)
                    {
                        DebugConsole.PrintError(MissingNearestWaypoint);
                        Input(new Input.ReturnToIdle());
                        return;
                    }

                    data.CurrentWaypoint = nearestWaypoint;
                    
                    var distance = enemyPosition.DistanceTo(nearestWaypoint.GlobalPosition);
                    if (distance > 2f)
                    {
                        // TODO: This might be a limitation from the SVO. The waypoint and closest voxel are more than 2.0 units apart.
                        DebugConsole.PrintError(DistanceTooGreat);
                        // We return here. Instead of getting a new waypoint,
                        // the state will continue until we are at the current target.
                        return;
                    }
                }
                if (data.CurrentWaypoint.Connections.Count == 0)
                {
                    DebugConsole.PrintError(MissingConnections);
                    Input(new Input.ReturnToIdle());
                    return;
                }
                    
                data.CurrentWaypoint = data.CurrentWaypoint.Connections.PickRandom();
                DebugConsole.Print(string.Format(MovingToWaypoint, data.CurrentWaypoint.Name));
            }

            private void GeneratePathToWaypoint()
            {
                var data = Get<Data>();
                var origin = Get<EnemyAIDrone>().GlobalPosition;
                var target = data.CurrentWaypoint.GlobalPosition;

                var path = GeneratePathway(origin, target);
                if (path.Length == 0)
                {
                    DebugConsole.PrintError(NoPathToWaypoint);
                    Input(new Input.ReturnToIdle());
                }
                else
                {
                    data.SVOPath = path;
                    data.CurrentPathIndex = 1; // We skip the first point since it's where we are at
                }
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                OnMovingAlongPath(input.Delta, Get<EnemyDroneSettings>().MaxSpeed, onFinalTargetReached: () => Input(new Input.StartScanning()));
                return ToSelf();
            }

            public Transition On(in Input.StartScanning input) => To<Scanning>();
        }
    }
}
