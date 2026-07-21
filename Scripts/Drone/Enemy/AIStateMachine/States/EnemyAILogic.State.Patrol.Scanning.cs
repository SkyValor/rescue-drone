namespace RescueDrone;

using System;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class EnemyAILogic
{
    public partial record State
    {
        [Meta]
        public partial record Scanning : Patrol, IGet<Input.Moved>, IGet<Input.PhysicsTick>
        {
            private int currentScanCount;
            private float currentScanTime;
            private Vector3 currentLookDirection;
            private Random random;
            private bool isScanning;
            
            public Scanning()
            {
                this.OnEnter(() =>
                {
                    random ??= new Random();
                    SetRandomLookingDirection();
                    currentScanCount = 0;
                    currentScanTime = 0f;
                });
            }

            public Transition On(in Input.PhysicsTick input)
            {
                var settings = Get<Settings>();
                if (isScanning)
                {
                    currentScanTime += (float) input.Delta;
                    if (currentScanTime < settings.ScanWaitTime) return ToSelf();

                    if (++currentScanCount < settings.NumberOfScans)
                    {
                        SetRandomLookingDirection();
                        currentScanTime = 0f;
                        isScanning = false;
                    }
                    else
                    {
                        return To<ToNextWaypoint>();
                    }
                }
                
                var enemy = Get<Mover>();
                SmoothlyRotate(enemy, currentLookDirection, settings.TurnSpeed, (float) input.Delta);
                return ToSelf();
            }

            public Transition On(in Input.Moved input)
            {
                if (isScanning) return ToSelf();

                // Check if enemy drone's nose is aligned with target direction
                var enemy = Get<Mover>();
                var frontDirection = -enemy.Basis.Z;
                isScanning = frontDirection.IsEqualApprox(currentLookDirection);
                return ToSelf();
            }

            private void SetRandomLookingDirection()
            {
                // Generate a random horizontal angle (Yaw) between -180 and 180 degrees
                var rotationX = (float) (random.NextDouble() * 2.0 * Mathf.Pi - Mathf.Pi);
                    
                // Generate a random vertical angle (Pitch) between -30 and 30 degrees
                var rotationY = Mathf.DegToRad(random.Next(-30, 30));
                var newLookDirection = ConvertSphericalAngleTo3DForward(rotationX, rotationY);
                    
                currentLookDirection = newLookDirection;
            }

            // Convert these spherical angles into a 3D forward direction vector
            private static Vector3 ConvertSphericalAngleTo3DForward(float rotationX, float rotationY)
            {
                return new Vector3(
                    Mathf.Sin(rotationX) * Mathf.Cos(rotationY),
                    Mathf.Sin(rotationY),
                    Mathf.Cos(rotationX) * Mathf.Cos(rotationY)).Normalized();
            }
        }
    }
}
