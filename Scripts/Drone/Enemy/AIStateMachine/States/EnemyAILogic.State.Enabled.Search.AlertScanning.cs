namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record AlertScanning : Search, IGet<Input.Moved>
        {
            public AlertScanning()
            {
                this.OnEnter(() => OnEnterScan(Get<EnemyDroneSettings>().SearchScanSettings));
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                return OnPhysicsTickScan(input, Get<EnemyDroneSettings>().SearchScanSettings);
            }

            public Transition On(in Input.Moved input) => OnMovedScan();
        }
    }
}
