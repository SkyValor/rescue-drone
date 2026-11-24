namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record RamCooldown : Chase
        {
            private float elapsedTime;

            public RamCooldown()
            {
                this.OnEnter(() => elapsedTime = 0f);
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                elapsedTime += (float) input.Delta;

                var cooldown = Get<EnemyDroneSettings>().RamCooldownDuration;
                if (elapsedTime < cooldown) return ToSelf();

                var data = Get<Data>();
                if (data.PlayerDetected)
                {
                    // Setting the repath position to infinite will ensure the Repath() will trigger
                    // at the start of a new frame in Pursuit state
                    data.LastRepathPosition = Vector3.Inf;
                    return To<Pursuit>();
                }

                return To<Fetch>();
            }
        }
    }
}
