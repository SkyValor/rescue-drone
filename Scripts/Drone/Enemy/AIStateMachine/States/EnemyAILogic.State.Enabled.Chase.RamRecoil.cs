namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record RamRecoil : Chase, IGet<Input.TriggerRamCooldown>
        {
            private Tween recoilTween;
            
            public RamRecoil()
            {
                this.OnEnter(() =>
                {
                    Output(new Output.VelocityComputed(Vector3.Zero));
                    SetupRecoilPath();
                    SetupRecoilTween();
                });
            }
            
            private void SetupRecoilPath()
            {
                var data = Get<Data>();
                var recoilDirection = (data.RamDirection.Inverse() with { Y = 0f }).Normalized();
                var recoilDistance = Get<EnemyDroneSettings>().RamRecoilDistance;
                
                var originPosition = Get<EnemyAIDrone>().GlobalPosition;
                var preferredPosition = originPosition + recoilDirection * recoilDistance;

                var pathway = GeneratePathwayUpTo(originPosition, preferredPosition);
                if (pathway.Length <= 1)
                {
                    DebugConsole.PrintWarning("RamRecoil Recoil pathway not available.");
                    Input(new Input.TriggerRamCooldown());
                    return;
                }
                
                data.SvoPath = pathway;
                data.CurrentPathIndex = 1;
            }
            
            private void SetupRecoilTween()
            {
                var data = Get<Data>();
                var destination = data.SvoPath[data.CurrentPathIndex];
                var duration = Get<EnemyDroneSettings>().RamRecoilDuration;
                var enemy = Get<EnemyAIDrone>();
                
                recoilTween = enemy.CreateTween();
                recoilTween
                    .TweenProperty(enemy, Node3D.PropertyName.GlobalPosition.ToString(), destination, duration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Quart);
                recoilTween.Play();
            }
            
            /// <summary>
            /// Backs away without rotation, in the opposite direction of ramming.
            /// The speed interpolates back to zero quickly.
            /// </summary>
            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                if (recoilTween.IsRunning()) return ToSelf();

                Input(new Input.TriggerRamCooldown());
                return ToSelf();
            }
            
            public Transition On(in Input.TriggerRamCooldown input) => To<RamCooldown>();
            
        }
    }
}
