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
            public Action OnLookoutFinishedAction { get; set; }
            public Type OnLookoutFinishedNextState { get; set; }
            
            private float initialY;
            private Tween rotationTween;
            
            public Lookout()
            {
                this.OnEnter(() =>
                {
                    initialY = Get<EnemyDrone>().RotationDegrees.Y;
                    Input(new Input.InitiateRotatingLeft());
                });
                this.OnExit(() =>
                {
                    OnLookoutFinishedNextState = null;
                    if (rotationTween != null && rotationTween.IsValid())
                        rotationTween.Kill();
                });
            }

            public Transition On(in Input.PhysicsTick input)
            {
                var player = Get<Drone>();
                var enemy = Get<EnemyDrone>();
                var settings = Get<Settings>();
                if (PlayerIsInLineOfSight(enemy, player, settings))
                {
                    Get<Data>().LastPlayerKnownPosition = player.GlobalPosition;
                    return To<Attack>();
                }
                
                DebugDraw3D.DrawLine(enemy.GlobalPosition, Get<Data>().LastPlayerKnownPosition, Colors.Brown);

                return ToSelf();
            }

            public Transition On(in Input.InitiateRotatingLeft input)
            {
                var enemy = Get<EnemyDrone>();
                var targetRotation = enemy.RotationDegrees with { Y = initialY + LookoutAngle / 2f };
                var onTweenFinished = Callable.From(() => Input(new Input.InitiateRotatingRight()));
                TweenLookout(enemy, targetRotation, LookoutRotationTime, onTweenFinished);
                return ToSelf();
            }

            public Transition On(in Input.InitiateRotatingRight input)
            {
                var enemy = Get<EnemyDrone>();
                var targetRotation = enemy.RotationDegrees with { Y = initialY - LookoutAngle / 2f };
                var onTweenFinished = Callable.From(() => Input(new Input.FinishedLookout()));
                TweenLookout(enemy, targetRotation, LookoutRotationTime * 2f, onTweenFinished);
                return ToSelf();
            }

            public Transition On(in Input.FinishedLookout input)
            {
                OnLookoutFinishedAction?.Invoke();
                if (OnLookoutFinishedNextState == typeof(Patrol))
                    return To<Patrol>();
                
                return OnLookoutFinishedNextState == typeof(Search) ? To<Search>() : To<Idle>();
            }

            private void TweenLookout(EnemyDrone enemy, Vector3 targetRotation, float duration, Callable onTweenFinished)
            {
                rotationTween = enemy.CreateTween();
                rotationTween.TweenProperty(enemy, "rotation_degrees", targetRotation, duration);
                rotationTween.TweenInterval(LookoutHoldDuration);
                rotationTween.TweenCallback(onTweenFinished);
                rotationTween.Play();
            }
            
        }
    }
}
