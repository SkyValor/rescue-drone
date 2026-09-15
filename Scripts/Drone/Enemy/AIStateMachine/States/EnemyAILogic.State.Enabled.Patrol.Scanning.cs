namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Scanning : Patrol, IGet<Input.Moved>
        {
            public Scanning()
            {
                this.OnEnter(() => OnEnterScan(Get<EnemyDroneSettings>().PatrolScanSettings));
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                return OnPhysicsTickScan(input, Get<EnemyDroneSettings>().PatrolScanSettings);
            }

            public Transition On(in Input.Moved input) => OnMovedScan();
        }
    }
}
