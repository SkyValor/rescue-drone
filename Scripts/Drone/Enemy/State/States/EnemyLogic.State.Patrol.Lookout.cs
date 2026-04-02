namespace RescueDrone;

using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public partial record Patrol
        {
            public record Lookout : Patrol, 
                // IGet<Input.PhysicsTick>, 
                IGet<Input.InitiateRotatingLeft>,
                IGet<Input.InitiateRotatingRight>, 
                IGet<Input.FinishedLookout>
            {
                public enum LookoutDirection { Left, Right }
                public LookoutDirection Direction;

                private readonly float minRadian = Mathf.DegToRad(5f);
                private IEnemyDrone drone;
                private float lookoutTimeRemaining;
                private Vector3 targetLookoutLeft;
                private Vector3 targetLookoutRight;

                private float initialY;
                private Tween rotationTween;
                
                public Lookout()
                {
                    this.OnEnter(() =>
                    {
                        GD.Print("Lookout");

                        initialY = Get<EnemyDrone>().RotationDegrees.Y;
                        Input(new Input.InitiateRotatingLeft());
                        
                        // Direction = LookoutDirection.Left;
                        // drone = Get<IEnemyDrone>();
                        // var currentRotation = drone.GlobalRotation;
                        // var lookoutAngle = Get<Settings>().LookoutAngle;
                        // targetLookoutLeft = currentRotation.Rotated(Vector3.Up, Mathf.DegToRad(-lookoutAngle));
                        // targetLookoutRight = currentRotation.Rotated(Vector3.Up, Mathf.DegToRad(lookoutAngle));
                        // lookoutTimeRemaining = Get<Settings>().LookoutDuration;
                    });
                    
                    this.OnExit(() =>
                    {
                        if (rotationTween is not null && rotationTween.IsValid())
                            rotationTween.Kill();
                    });
                }

                public Transition On(in Input.InitiateRotatingLeft input) 
                {
                    var enemy = Get<EnemyDrone>();
                    var settings = Get<Settings>();
                    var targetRotation = enemy.RotationDegrees with { Y = initialY + settings.LookoutAngle };
                    
                    rotationTween = enemy.CreateTween();
                    rotationTween.TweenProperty(enemy, "rotation_degrees", targetRotation, settings.LookoutRotationTime);
                    rotationTween.TweenInterval(settings.LookoutHoldDuration);
                    rotationTween.TweenCallback(Callable.From(() => Input(new Input.InitiateRotatingRight())));
                    rotationTween.Play();
                    return ToSelf();
                }

                public Transition On(in Input.InitiateRotatingRight input)
                {
                    var enemy = Get<EnemyDrone>();
                    var settings = Get<Settings>();
                    var targetRotation = enemy.RotationDegrees with { Y = initialY - settings.LookoutAngle };

                    rotationTween = enemy.CreateTween();
                    rotationTween.TweenProperty(enemy, "rotation_degrees", targetRotation, settings.LookoutRotationTime);
                    rotationTween.TweenInterval(settings.LookoutHoldDuration);
                    rotationTween.TweenCallback(Callable.From(() => Input(new Input.FinishedLookout())));
                    rotationTween.Play();
                    return ToSelf();
                }

                // public Transition On(in Input.PhysicsTick input)
                // {
                //     if (Direction is LookoutDirection.Left)
                //     {
                //         var remainingAngle = drone.GlobalRotation.AngleTo(targetLookoutLeft);
                //         if (remainingAngle > minRadian)
                //         {
                //             Output(new Output.RotationRequest(targetLookoutLeft, input.DeltaTime));
                //             return ToSelf();
                //         }
                //         
                //         lookoutTimeRemaining -= input.DeltaTime;
                //         if (lookoutTimeRemaining > 0f)
                //             return ToSelf();
                //         
                //         Direction = LookoutDirection.Right;
                //         lookoutTimeRemaining = Get<Settings>().LookoutHoldDuration;
                //     }
                //     
                //     if (Direction is LookoutDirection.Right)
                //     {
                //         var remainingAngle = drone.GlobalRotation.AngleTo(targetLookoutRight);
                //         if (remainingAngle > minRadian)
                //         {
                //             Output(new Output.RotationRequest(targetLookoutRight, input.DeltaTime));
                //             return ToSelf();
                //         }
                //
                //         lookoutTimeRemaining -= input.DeltaTime;
                //         if (lookoutTimeRemaining > 0f)
                //             return ToSelf();
                //     }
                //     
                //     Input(new Input.FinishedLookout());
                //     return ToSelf();
                // }

                public Transition On(in Input.FinishedLookout input)
                {
                    var nextWaypoint = GetNextWaypoint();
                    previousWaypoint = currentWaypoint;
                    currentWaypoint = nextWaypoint;
                    return To<GoToWaypoint>();
                }
                
            }
        }
    }
}
