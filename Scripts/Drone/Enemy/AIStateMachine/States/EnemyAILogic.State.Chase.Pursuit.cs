namespace RescueDrone;

using Chickensoft.Introspection;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Pursuit : Chase, IGet<Input.PlayerDroneTooClose>, IGet<Input.PlayerDroneCloseEnough>
        {
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var player = Get<IGameRepo>().Player.Value;
                var settings = Get<Settings>();
                
                var idealTarget = CalculatePursuitTarget(enemy, player, settings);
                if (idealTarget.DistanceTo(data.LastPlayerPosition) <= settings.RepathThreshold) 
                    return ToSelf();
                
                data.LastPlayerPosition = idealTarget;

                var pathfinder = Get<IDronePathfindingSVO>();
                var path = pathfinder.FindPath(enemy.GlobalPosition, idealTarget);
                
                if (path.Count <= 0) return ToSelf();
                
                data.SVOPath = path;
                data.CurrentPathIndex = 1;
                return ToSelf();
            }

            public Transition On(in Input.PlayerDroneTooClose input) => To<Retreat>();

            public Transition On(in Input.PlayerDroneCloseEnough input) => To<Stay>();
            
        }
    }
}
