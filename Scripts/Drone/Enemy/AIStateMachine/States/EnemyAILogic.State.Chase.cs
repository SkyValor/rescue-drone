namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// 
        /// </summary>
        [Meta]
        public partial record Chase : State, IGet<Input.PhysicsTick>
        {
            public Chase()
            {
                OnAttach(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.DroneOnSight += OnPlayerInSight;
                });
                
                OnDetach(() =>
                {
                    var sight = Get<SightSensor>();
                    sight.DroneOnSight -= OnPlayerInSight;
                });
            }

            private void OnPlayerInSight(IFlyingDrone playerDrone)
            {
                if (playerDrone is not Node3D playerNode) return;
                
                Get<Data>().LastPlayerPosition = playerNode.GlobalRotation;
                Input(new Input.PlayerInSight());
            }
            
            public virtual Transition On(in Input.PhysicsTick input)
            {
                CheckDistanceToPlayer();
                CheckPlayerInSight();
                return ToSelf();
            }

            /// <summary>
            /// Check the current distance from this drone to the player's last registered position.
            /// </summary>
            private void CheckDistanceToPlayer()
            {
                var settings = Get<EnemyDroneSettings>();
                var enemy = Get<EnemyAIDrone>();
                var data = Get<Data>();
                
                var distanceToPlayer = enemy.GlobalPosition.DistanceTo(data.LastPlayerPosition);
                if (distanceToPlayer < settings.MinDistance)
                    Input(new Input.PlayerDroneTooClose());
                else if (distanceToPlayer > settings.MaxDistance)
                    Input(new Input.PlayerDroneTooFar());
                else
                    Input(new Input.PlayerDroneCloseEnough());
            }

            private void CheckPlayerInSight()
            {
                var sight = Get<SightSensor>();
                var player = Get<IGameRepo>().PlayerDrone.Value;
                
                if (sight.TargetInSight(player))
                    Input(new Input.PlayerInSight());
                else
                    Input(new Input.LostSightOfPlayer());
            }

            public virtual Transition On(in Input.PlayerInSight input)
            {
                var data = Get<Data>();
                var player = Get<IGameRepo>().PlayerDrone.Value;
                data.LastPlayerPosition = player.GlobalPosition;
                data.PlayerDetectedThisFrame = true;
                return ToSelf();
            }

            public Transition On(in Input.LostSightOfPlayer input)
            {
                var data = Get<Data>();
                data.PlayerDetectedThisFrame = false;
                return ToSelf();
            }
            
        }
    }
}
