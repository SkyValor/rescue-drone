namespace RescueDrone;

using System;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyLogic
{
    public partial record State
    {
        public record Lookout : State, 
            IGet<Input.PhysicsTick>, 
            IGet<Input.InitiateRotatingLeft>, 
            IGet<Input.InitiateRotatingRight>,
            IGet<Input.FinishedLookout>
        {
            public float LookoutAngle { get; set; }
            public float LookoutRotationTime { get; set; }
            public float LookoutHoldDuration { get; set; }
            public Func<Transition> OnLookoutFinished { get; set; }
            
            private readonly float initialY;
            private Tween rotationTween;
            
            public Lookout()
            {
                this.OnEnter(() => GD.Print("SearchLookout"));
                initialY = Get<EnemyDrone>().RotationDegrees.Y;
                Input(new Input.InitiateRotatingLeft());
            }

            public Transition On(in Input.PhysicsTick input)
            {
                var player = Get<Drone>();
                var enemy = Get<EnemyDrone>();
                var settings = Get<Settings>();
                if (PlayerIsInLineOfSight(enemy, player, settings))
                {
                    GD.Print("Player is in line of sight!");
                    Get<Data>().LastPlayerKnownPosition = player.GlobalPosition;
                    return To<Attack>();
                }

                return ToSelf();
            }

            public Transition On(in Input.InitiateRotatingLeft input)
            {
                var enemy = Get<EnemyDrone>();
                var targetRotation = enemy.RotationDegrees with { Y = initialY + LookoutAngle };
                var onTweenFinished = Callable.From(() => Input(new Input.InitiateRotatingRight()));
                TweenLookout(enemy, targetRotation, onTweenFinished);
                return ToSelf();
            }

            public Transition On(in Input.InitiateRotatingRight input)
            {
                var enemy = Get<EnemyDrone>();
                var targetRotation = enemy.RotationDegrees with { Y = initialY - LookoutAngle };
                var onTweenFinished = Callable.From(() => Input(new Input.FinishedLookout()));
                TweenLookout(enemy, targetRotation, onTweenFinished);
                return ToSelf();
            }

            public Transition On(in Input.FinishedLookout input) => OnLookoutFinished?.Invoke() ?? To<Idle>();

            private void TweenLookout(EnemyDrone enemy, Vector3 targetRotation, Callable onTweenFinished)
            {
                rotationTween = enemy.CreateTween();
                rotationTween.TweenProperty(enemy, "rotation_degrees", targetRotation, LookoutRotationTime);
                rotationTween.TweenInterval(LookoutHoldDuration);
                rotationTween.TweenCallback(onTweenFinished);
                rotationTween.Play();
            }
            
        }
    }
}
