namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    [Meta]
    public partial record State : StateLogic<State>
    {
        private static bool HasLineOfSight(EnemyDrone enemy, Drone player, Settings settings)
        {
            return 
                PlayerInRange(enemy, player, settings.VisionRange) && 
                PlayerInVisionRange(enemy, player) && 
                NoBuildingInBetween(enemy, player);
        }

        private static bool PlayerInRange(EnemyDrone enemy, Drone player, float visionRange)
        {
            var distanceToPlayer = enemy.GlobalPosition.DistanceTo(player.GlobalPosition);
            return distanceToPlayer <= visionRange;
        }

        private static bool PlayerInVisionRange(EnemyDrone enemy, Drone player)
        {
            var forward = -enemy.Basis.Z;
            var directionToPlayer = enemy.GlobalPosition.DirectionTo(player.GlobalPosition);
            return Mathf.RadToDeg(directionToPlayer.AngleTo(forward)) <= 90f / 2;
        }

        private static bool NoBuildingInBetween(EnemyDrone enemy, Drone player)
        {
            enemy.VisionRaycast.LookAt(player.GlobalPosition, Vector3.Up);
            enemy.VisionRaycast.ForceRaycastUpdate();
            return !enemy.VisionRaycast.IsColliding();
        }
    }
}
