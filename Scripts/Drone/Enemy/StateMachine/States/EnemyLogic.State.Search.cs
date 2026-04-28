namespace RescueDrone;

using System;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public record Search : State, IGet<Input.PhysicsTick>
        {
            public Search()
            {
                this.OnEnter(() => GD.Print("Search"));
            }

            public Transition On(in Input.PhysicsTick input)
            {
                var data = Get<Data>();
                var player = Get<Drone>();
                var enemy = Get<EnemyDrone>();
                var settings = Get<Settings>();
                if (PlayerIsInLineOfSight(enemy, player, settings))
                {
                    data.LastPlayerKnownPosition = player.GlobalPosition;
                    return To<Attack>();
                }

                DebugDraw3D.DrawLine(enemy.GlobalPosition, data.LastPlayerKnownPosition, Colors.Brown);
                
                var distanceToKnownPosition = enemy.GlobalPosition.DistanceTo(data.LastPlayerKnownPosition);
                if (distanceToKnownPosition < 0.05f)
                {
                    return To<Lookout>().With(SetupLookoutState(settings));
                }

                Output(new Output.MoveTowards(data.LastPlayerKnownPosition, input.DeltaTime));
                return ToSelf();
            }

            private static Action<State> SetupLookoutState(Settings settings)
            {
                return state =>
                {
                    var lookoutState = (Lookout) state;
                    lookoutState.LookoutAngle = settings.SearchLookoutAngle;
                    lookoutState.LookoutRotationTime = settings.SearchLookoutRotationTime;
                    lookoutState.LookoutHoldDuration = settings.SearchLookoutHoldDuration;
                    lookoutState.OnLookoutFinishedNextState = typeof(Idle);
                };
            }
            
        }
    }
}
