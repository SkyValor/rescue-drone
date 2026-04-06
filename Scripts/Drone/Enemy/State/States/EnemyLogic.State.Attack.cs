namespace RescueDrone;

using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public record Attack : State, IGet<Input.PhysicsTick>
        {
            private bool playerOnSight;
            
            public Attack()
            {
                this.OnEnter(() => GD.Print("Attack"));
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
                    playerOnSight = true;
                }
                else
                {
                    playerOnSight = false;
                }

                var rayColor = playerOnSight ? Colors.Green : Colors.Brown;
                DebugDraw3D.DrawLine(enemy.GlobalPosition, data.LastPlayerKnownPosition, rayColor);

                var distanceToKnownPosition = enemy.GlobalPosition.DistanceTo(data.LastPlayerKnownPosition);
                if (playerOnSight && distanceToKnownPosition > settings.PlayerMinDistance)
                {
                    Output(new Output.MoveTowards(data.LastPlayerKnownPosition, input.DeltaTime));
                    return ToSelf();
                }
                
                if (!playerOnSight)
                {
                    return To<Search>();
                }

                return ToSelf();
            }
            
        }
    }
}
