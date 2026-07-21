namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;
using Godot.Collections;

public partial class EnemyAILogic
{
    [Meta]
    public partial record State : StateLogic<State>
    {
        private void ComputeMovement(Mover enemy, Vector3 targetPosition, float maxSpeed, float deltaTime)
        {
            var data = Get<Data>();
            var settings = Get<Settings>();
            
            var desiredSpeed = CalculateCurveSpeed(data.SVOPath, data.CurrentPathIndex, enemy.GlobalPosition, 
                maxSpeed, settings.BreakingDistance, settings.MinTurnSpeedPercentage);
            var speedBleedingFactor = desiredSpeed < data.CurrentTargetSpeed ? settings.Acceleration : settings.Deceleration;
            data.CurrentTargetSpeed = Mathf.Lerp(data.CurrentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);
            
            var toTarget = targetPosition - enemy.GlobalPosition;
            var direction = toTarget.Normalized();
            SmoothlyRotate(enemy, direction, settings.TurnSpeed, deltaTime);

            var velocity = enemy.Velocity;
            var targetVelocity = direction * data.CurrentTargetSpeed;
            velocity = velocity.Lerp(targetVelocity, settings.Acceleration * deltaTime);

            Output(new Output.VelocityComputed(velocity));
        }

        private void SmoothlyRotate(Mover enemy, Vector3 toDirection, float turnSpeed, float deltaTime)
        {
            if (toDirection == Vector3.Zero) return;

            var targetBasis = Basis.LookingAt(toDirection, Vector3.Up);
            var globalTrans = enemy.GlobalTransform;
            var globalPos = enemy.GlobalPosition;
            globalTrans = globalTrans.InterpolateWith(new Transform3D(targetBasis, globalPos), turnSpeed * deltaTime);
            Output(new Output.RotationComputed(globalTrans));
        }

        private static float CalculateCurveSpeed(Array<Vector3> path, int pathIndex, Vector3 currentPosition,
            float maxSpeed, float breakingDistance, float minTurnSpeedPercentage)
        {
            if (path is null || pathIndex == 0 || pathIndex == path.Count - 1)
                return maxSpeed;
            
            var pointA = path[pathIndex - 1];
            var pointB = path[pathIndex];
            var pointC = path[pathIndex + 1];

            var incomingDir = (pointB - pointA).Normalized();
            var outgoingDir = (pointC - pointB).Normalized();

            var dot = incomingDir.Dot(outgoingDir);
            var turnSmoothness = Mathf.Remap(dot, -1f, 1f, 0f, 1f);
            var targetSpeedForTurn = Mathf.Lerp(maxSpeed * minTurnSpeedPercentage, maxSpeed, turnSmoothness);

            return CalculateSpeedNearTarget(currentPosition, pointB, targetSpeedForTurn, maxSpeed, breakingDistance);
        }

        /// <summary>
        /// Calculate the speed as we approach the designated target. The returned value will be interpolated
        /// between <c>speedAtTarget</c> and <c>startingSpeed</c>, depending on how close we are to the target.
        /// </summary>
        /// <param name="current">current global position</param>
        /// <param name="target">target global position</param>
        /// <param name="startingSpeed">which speed is desired when we are on the target</param>
        /// <param name="maxSpeed">which speed is desired before we reach the breaking distance</param>
        /// <param name="breakingDistance">distance from the target at which we start lowering speed</param>
        /// <returns></returns>
        private static float CalculateSpeedNearTarget(Vector3 current, Vector3 target, float startingSpeed, float maxSpeed, float breakingDistance)
        {
            var distanceToDestination = current.DistanceTo(target);
            if (distanceToDestination >= breakingDistance) 
                return maxSpeed;
            
            var t = Mathf.Clamp(distanceToDestination / breakingDistance, 0f, 1f);
            return Mathf.Lerp(startingSpeed, maxSpeed, t);
        }
    }
}
