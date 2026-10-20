namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Scanning : Patrol, IGet<Input.Moved>, IGet<Input.ScanningComplete>
        {
            public Scanning()
            {
                this.OnEnter(() => OnEnterScan(Get<EnemyDroneSettings>().PatrolScanSettings));
                this.OnExit(() => Get<Data>().IsScanning = false);
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                return OnPhysicsTickScan(input, Get<EnemyDroneSettings>().PatrolScanSettings);
            }

            public Transition On(in Input.Moved input) => OnMovedScan();

            public Transition On(in Input.ScanningComplete input) => To<ToPatrolWaypoint>();
        }
        
    }
}
