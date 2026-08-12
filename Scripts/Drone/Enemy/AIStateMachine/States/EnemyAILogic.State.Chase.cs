namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Chase : State, IGet<Input.PhysicsTick>, IGet<Input.PlayerInSight>, IGet<Input.LostSightOfPlayer>
        {
            public Chase()
            {
                this.OnEnter(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.PlayerInSight += OnPlayerInSight;
                    sight.LostSightOfPlayer += OnLostSightOfPlayer;
                });
                
                this.OnExit(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.PlayerInSight -= OnPlayerInSight;
                    sight.LostSightOfPlayer -= OnLostSightOfPlayer;
                });
            }

            private void OnPlayerInSight(Vector3 playerPosition)
            {
                Get<Data>().LastPlayerPosition = playerPosition;
                Input(new Input.PlayerInSight());
            }

            private void OnLostSightOfPlayer()
            {
                Input(new Input.LostSightOfPlayer());
            }
            
            public virtual Transition On(in Input.PhysicsTick input)
            {
                CheckDistanceToPlayer();
                CheckPlayerInSight();
                return ToSelf();
            }

            private void CheckDistanceToPlayer()
            {
                var enemy = Get<EnemyAIDrone>();
                var data = Get<Data>();
                
                var distanceToPlayer = enemy.GlobalPosition.DistanceTo(data.LastPlayerPosition);
                if (distanceToPlayer < enemy.MinDistance)
                    Input(new Input.PlayerDroneTooClose());
                else if (distanceToPlayer > enemy.MaxDistance)
                    Input(new Input.PlayerDroneTooFar());
                else
                    Input(new Input.PlayerDroneCloseEnough());
            }

            private void CheckPlayerInSight()
            {
                var sight = Get<SightSensor>();
                var player = Get<IGameRepo>().Player.Value;
                
                if (sight.TargetInSight(player))
                    Input(new Input.PlayerInSight());
                else
                    Input(new Input.LostSightOfPlayer());
            }

            public virtual Transition On(in Input.PlayerInSight input)
            {
                var data = Get<Data>();
                var player = Get<IGameRepo>().Player.Value;
                data.LastPlayerPosition = player.GlobalPosition;
                data.PlayerSeenLastFrame = true;
                return ToSelf();
            }

            public Transition On(in Input.LostSightOfPlayer input)
            {
                var data = Get<Data>();
                data.PlayerSeenLastFrame = false;
                return ToSelf();
            }
            
        }
    }
}
