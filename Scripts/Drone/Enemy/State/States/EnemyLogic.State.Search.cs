namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Search : State, IGet<Input.PhysicsTick>
        {
            public Search()
            {
                this.OnEnter(() => GD.Print("Search"));
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

                var playerKnownPosition = Get<Data>().LastPlayerKnownPosition;
                var distanceToKnownPosition = enemy.GlobalPosition.DistanceTo(playerKnownPosition);
                if (distanceToKnownPosition < 0.05f)
                {
                    return To<Lookout>().With(state =>
                    {
                        var lookoutState = (Lookout) state;
                        lookoutState.LookoutAngle = settings.SearchLookoutAngle;
                        lookoutState.LookoutRotationTime = settings.SearchLookoutRotationTime;
                        lookoutState.LookoutHoldDuration = settings.SearchLookoutHoldDuration;
                        lookoutState.OnLookoutFinished = To<Idle>;
                    });
                }

                Output(new Output.MoveTowards(playerKnownPosition, input.DeltaTime));
                return ToSelf();
            }
            
        }
    }
}
