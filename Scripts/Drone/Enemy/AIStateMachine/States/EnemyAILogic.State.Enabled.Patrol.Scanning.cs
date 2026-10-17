namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Scanning : Patrol, IGet<Input.Moved>, IGet<Input.ScanningComplete>
        {
            public Scanning()
            {
                OnAttach(() => DebugConsole.Print("=> Enter state Scanning"));
                this.OnEnter(() => OnEnterScan(Get<EnemyDroneSettings>().PatrolScanSettings));
                this.OnExit(() => Get<Data>().IsScanning = false);
            }

            public override Transition On(in Input.PhysicsTick input)
            {
                base.On(input);
                
                var data = Get<Data>();
                var enemy = Get<EnemyAIDrone>();
                var scanDirection = data.ScanDirection;
                var enemyPosition = enemy.GlobalPosition;
                var enemyLookDir = -enemy.Basis.Z;
                var waypointLookDir = -data.CurrentWaypoint.Basis.Z;
                
                DebugDraw3D.DrawLine(enemyPosition, enemyPosition + waypointLookDir * 2f, Colors.Goldenrod);
                DebugDraw3D.DrawLine(enemyPosition, enemyPosition + enemyLookDir * 2f, Colors.Maroon);
                DebugDraw3D.DrawLine(enemyPosition, enemyPosition + scanDirection * 2f, Colors.Blue);
                
                return OnPhysicsTickScan(input, Get<EnemyDroneSettings>().PatrolScanSettings);
            }

            public Transition On(in Input.Moved input) => OnMovedScan();

            public Transition On(in Input.ScanningComplete input) => To<ToPatrolWaypoint>();
        }
        
    }
}
