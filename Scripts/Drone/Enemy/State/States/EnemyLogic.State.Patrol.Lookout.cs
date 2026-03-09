namespace RescueDrone;

using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public partial record Patrol
        {
            public record Lookout : Patrol, IGet<Input.PhysicsTick>, IGet<Input.FinishedLookout>
            {
                public enum LookoutDirection { Left, Right }
                public LookoutDirection Direction;

                private readonly float minRadian = Mathf.DegToRad(5f);
                private IEnemyDrone drone;
                private float lookoutTimeRemaining;
                private Vector3 targetLookoutLeft;
                private Vector3 targetLookoutRight;
                
                public Lookout()
                {
                    this.OnEnter(() =>
                    {
                        GD.Print("Patrol state - On lookout");
                        Direction = LookoutDirection.Left;
                        drone = Get<IEnemyDrone>();
                        var currentRotation = drone.GlobalRotation;
                        var lookoutAngle = Get<Settings>().LookoutAngle;
                        targetLookoutLeft = currentRotation.Rotated(Vector3.Up, Mathf.DegToRad(-lookoutAngle));
                        targetLookoutRight = currentRotation.Rotated(Vector3.Up, Mathf.DegToRad(lookoutAngle));
                        lookoutTimeRemaining = Get<Settings>().LookoutDuration;
                    });
                }
                
                public Transition On(in Input.PhysicsTick input)
                {
                    if (Direction is LookoutDirection.Left)
                    {
                        var remainingAngle = drone.GlobalRotation.AngleTo(targetLookoutLeft);
                        if (remainingAngle > minRadian)
                        {
                            Output(new Output.RotationRequest(targetLookoutLeft, input.DeltaTime));
                            return ToSelf();
                        }
                        
                        lookoutTimeRemaining -= input.DeltaTime;
                        if (lookoutTimeRemaining > 0f)
                            return ToSelf();
                        
                        Direction = LookoutDirection.Right;
                        lookoutTimeRemaining = Get<Settings>().LookoutDuration;
                    }
                    
                    if (Direction is LookoutDirection.Right)
                    {
                        var remainingAngle = drone.GlobalRotation.AngleTo(targetLookoutRight);
                        if (remainingAngle > minRadian)
                        {
                            Output(new Output.RotationRequest(targetLookoutRight, input.DeltaTime));
                            return ToSelf();
                        }

                        lookoutTimeRemaining -= input.DeltaTime;
                        if (lookoutTimeRemaining > 0f)
                            return ToSelf();
                    }
                    
                    Input(new Input.FinishedLookout());
                    return ToSelf();
                }

                public Transition On(in Input.FinishedLookout input)
                {
                    return To<GoToWaypoint>();
                }
                
            }
        }
    }
}
