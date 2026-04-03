namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        [Meta]
        public partial record Attack : State, IGet<Input.PhysicsTick>
        {
            public Vector3 LastPlayerKnownPosition = Vector3.Zero;
            private bool playerOnSight;
            
            public Attack()
            {
                this.OnEnter(() => GD.Print("Attack"));
            }

            public Transition On(in Input.PhysicsTick input)
            {
                var player = Get<Drone>();
                var enemy = Get<EnemyDrone>();
                var settings = Get<Settings>();
                if (PlayerIsInLineOfSight(enemy, player, settings))
                {
                    GD.Print("LineOfSight");
                    LastPlayerKnownPosition = player.GlobalPosition;
                    playerOnSight = true;
                }
                else
                {
                    GD.Print("No sight!");
                    playerOnSight = false;
                }

                var rayColor = playerOnSight ? Colors.Green : Colors.Brown;
                DebugDraw3D.DrawLine(enemy.GlobalPosition, LastPlayerKnownPosition, rayColor);

                enemy.DroneMovement.RotateSmoothlyTo(LastPlayerKnownPosition, input.DeltaTime);
                var distanceToPlayer = enemy.GlobalPosition.DistanceTo(LastPlayerKnownPosition);
                if (distanceToPlayer > settings.PlayerMinDistance)
                {
                    enemy.DroneMovement.MoveTo(LastPlayerKnownPosition, input.DeltaTime);
                }
                else if (!playerOnSight)
                {
                    return To<Patrol>();
                }

                return ToSelf();
            }
            
        }
    }
}
