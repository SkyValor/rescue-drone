namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    [Meta]
    public partial record State : StateLogic<State>
    {
        /// <summary>
        /// Get the state machine's current pathfinding instance and request to generate
        /// a point pathway from origin to target.
        /// </summary>
        /// <param name="originPosition"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private Vector3[] GeneratePathway(Vector3 originPosition, Vector3 targetPosition)
        {
            var world = Get<World3D>();
            var enemy = Get<EnemyAIDrone>();
            var pathfinder = Get<IPathfindSVO>();
            return pathfinder.CreatePath(originPosition, targetPosition, world, enemy.DroneRadius,
                [enemy.GetRid()]);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="targetPosition"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="deltaTime"></param>
        private void ComputeMovementAlongPath(EnemyAIDrone enemy, Vector3 targetPosition, float maxSpeed, float deltaTime)
        {
            var data = Get<Data>();
            var settings = Get<EnemyDroneSettings>();
            
            // Get the desired speed at the next point in this pathway and change the current speed
            // depending on the distance to said point.
            
            var currentSpeed = enemy.Velocity.Length();
            var desiredSpeed = CalculateCurveSpeed(data, settings, enemy.GlobalPosition, maxSpeed);
            var speedBleedingFactor = currentSpeed < desiredSpeed ? settings.Acceleration : settings.Deceleration;
            data.CurrentTargetSpeed = Mathf.Lerp(data.CurrentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);

            var toTarget = targetPosition - enemy.GlobalPosition;
            var direction = toTarget.Normalized();
            SmoothlyRotate(enemy, direction, settings.TurnSpeed, deltaTime);

            // Smoothly alter the current velocity towards the target speed and direction,
            // and output the result.
            
            var velocity = enemy.Velocity;
            var targetVelocity = direction * data.CurrentTargetSpeed;
            var velocityBleedingFactor = velocity.Length() < data.CurrentTargetSpeed 
                ? settings.Acceleration 
                : settings.Deceleration;
            
            velocity = velocity.Lerp(targetVelocity, velocityBleedingFactor * deltaTime);
            Output(new Output.VelocityComputed(velocity));
        }

        private void ComputeMovementWithoutRotation(EnemyAIDrone enemy, Vector3 toDirection, float desiredSpeed, float deltaTime)
        {
            var data = Get<Data>();
            var settings = Get<EnemyDroneSettings>();
            
            var speedBleedingFactor = desiredSpeed < data.CurrentTargetSpeed ? settings.Acceleration : settings.Deceleration;
            data.CurrentTargetSpeed = Mathf.Lerp(data.CurrentTargetSpeed, desiredSpeed, speedBleedingFactor * deltaTime);
            
            var velocity = enemy.Velocity;
            var targetVelocity = toDirection.Normalized() * data.CurrentTargetSpeed;
            velocity = velocity.Lerp(targetVelocity, settings.Deceleration * deltaTime);
            Output(new Output.VelocityComputed(velocity));
        }

        /// <summary>
        /// Compute the target rotation this drone should have when smoothly turning towards <c>toDirection</c>.
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="toDirection"></param>
        /// <param name="turnSpeed"></param>
        /// <param name="deltaTime"></param>
        private void SmoothlyRotate(EnemyAIDrone enemy, Vector3 toDirection, float turnSpeed, float deltaTime)
        {
            if (toDirection == Vector3.Zero) return;

            var targetBasis = Basis.LookingAt(toDirection, Vector3.Up);
            var globalTrans = enemy.GlobalTransform;
            var globalPos = enemy.GlobalPosition;
            globalTrans = globalTrans.InterpolateWith(new Transform3D(targetBasis, globalPos), turnSpeed * deltaTime);
            Output(new Output.RotationComputed(globalTrans));
        }

        /// <summary>
        /// Pinpoint the current location of the enemy drone inside the point pathway. If we are approaching a curve,
        /// then adjust the target speed accordingly. The target speed decreases from a combination of distance to said curve
        /// and how tight said curve is.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="settings"></param>
        /// <param name="currentPosition">current global position</param>
        /// <param name="maxSpeed">the maximum speed the drone can reach right now</param>
        /// <returns></returns>
        private static float CalculateCurveSpeed(Data data, EnemyDroneSettings settings, Vector3 currentPosition, float maxSpeed)
        {
            var path = data.SVOPath;
            var pathIndex = data.CurrentPathIndex;
            
            if (path is null || pathIndex == 0 || pathIndex == path.Length - 1)
                return maxSpeed;
            
            var pointA = path[pathIndex - 1];
            var pointB = path[pathIndex];
            var pointC = path[pathIndex + 1];

            // To get how tight a curve is, we calculate the dot product from the current direction
            // and next direction. The result is then remapped to a normalized value, in order to 
            // be able to be used as a weight in our lerp function from minimum speed and maximum speed.
            
            var incomingDir = (pointB - pointA).Normalized();
            var outgoingDir = (pointC - pointB).Normalized();
            
            var dot = incomingDir.Dot(outgoingDir);
            var turnTightness = Mathf.Remap(dot, -1f, 1f, 0f, 1f);
            var targetSpeedForTurn = Mathf.Lerp(maxSpeed * settings.MinTurnSpeedPercentage, maxSpeed, turnTightness);
            
            // We now have the target speed the enemy drone should have at the point of transition from one pathway point
            // to another. The actual speed is now dependent on how close we are to that position.

            return CalculateSpeedNearTarget(currentPosition, pointB, targetSpeedForTurn, maxSpeed, settings.BreakingDistance);
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
