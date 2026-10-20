namespace RescueDrone;

using System;
using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Fetch : Search, IGet<Input.ReturnToIdle>
        {
            public Fetch()
            {
                OnAttach(GeneratePathToPlayerLastPosition);
            }
            
            private void GeneratePathToPlayerLastPosition()
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();

                var origin = enemy.GlobalPosition;
                var target = data.LastKnownPlayerPosition;

                var path = GeneratePathway(origin, target);
                if (path.Length == 0)
                {
                    DebugConsole.PrintError("Error in Fetch: Pathfinder did not find a path to player drone's last position. Fallback to idle state.");
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
                ComputeMovementFetching(input.Delta);
                return ToSelf();
            }

            private void ComputeMovementFetching(double delta)
            {
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();

                try
                {
                    var targetPosition = data.SVOPath[data.CurrentPathIndex];
                    if (enemy.GlobalPosition.DistanceTo(targetPosition) < settings.CheckpointRadius)
                    {
                        if (data.CurrentPathIndex == data.SVOPath.Length - 1)
                            Input(new Input.StartAlertScanning());
                        else
                            data.CurrentPathIndex++;

                        return;
                    }
                    ComputeMovementAlongPath(enemy, targetPosition, settings.MaxSpeed, (float) delta);
                }
                catch (Exception ex)
                {
                    DebugConsole.PrintError("Error in try/catch Fetch: " + ex.Message);
                    Input(new Input.ReturnToIdle());
                }
            }

            public Transition On(in Input.ReturnToIdle input) => To<Idle>();
        }
    }
}
