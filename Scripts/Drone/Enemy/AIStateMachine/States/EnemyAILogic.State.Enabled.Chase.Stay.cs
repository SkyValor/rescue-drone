namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        /// <summary>
        /// The enemy drone maintains this position while looking at the player drone.
        /// 
        /// If the player drone gets too close, the state is changed to <see cref="State.Retreat"/>.
        /// Otherwise, if the player drone gets farther away, the state changes to <see cref="State.Pursuit"/>.
        /// </summary>
        [Meta]
        public partial record Stay : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneTooFar>
        {
            public Stay()
            {
                OnAttach(() => DebugConsole.Print("OnAttach state Stay"));
                this.OnEnter(() => Output(new Output.VelocityComputed(Vector3.Zero)));
            }
            
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var enemy = Get<EnemyAIDrone>();
                var settings = Get<EnemyDroneSettings>();
                var playerPosition = Get<Data>().DetectedPlayer.GlobalPosition;
                var toPlayer = enemy.GlobalPosition.DirectionTo(playerPosition) with { Y = 0f };
                
                TrackPlayerOutOfBounds(enemy.GlobalPosition, playerPosition, settings);
                SmoothlyRotate(enemy, toPlayer, settings.TurnSpeed, (float) input.Delta);
                return ToSelf();
            }
            
            // TODO: Finish the waypoint depth auto-subdivide
            
            // TODO: Track player out of bounds based on distance to sweet spot's voxel node rather than pure numbers

            private void TrackPlayerOutOfBounds(Vector3 enemyPosition, Vector3 playerPosition, EnemyDroneSettings settings)
            {
                var sweetSpot = (settings.MinDistance + settings.MaxDistance) * 0.5f;
                var distance = enemyPosition.DistanceTo(playerPosition);

                var errorAmount = distance - sweetSpot;
                if (errorAmount > 1f) Input(new Input.PlayerDroneTooFar());
                if (errorAmount < -0.75f) Input(new Input.PlayerDroneTooClose());
            }

            public Transition On(in Input.PlayerDroneTooClose input) => To<Retreat>();

            public Transition On(in Input.PlayerDroneTooFar input) => To<Pursuit>();
            
        }
    }
}
