namespace RescueDrone;

using Chickensoft.Introspection;
using Godot;

public partial class PlayerLogic
{
    public partial record State
    {
        [Meta]
        public abstract partial record Alive : State, IGet<Input.OnInputEvent>, IGet<Input.OnPhysicsTick>
        {
            public Transition On(in Input.OnInputEvent input)
            {
                if (input.Event.IsActionPressed(GameInputs.ToggleMouseCapture))
                    Output(new Output.ToggleMouseCapture());
                        
                return ToSelf();
            }

            public Transition On(in Input.OnPhysicsTick input)
            {
                var deltaTime = (float) input.Delta;
                var player = Get<PlayerTestScript>();
                var settings = Get<PlayerSettings>();
                var gameRepo = Get<IGameRepo>();

                var playerCamera = gameRepo.MainCamera.Value;
                var moveDirection = player.GetInputBasedOnCamera(playerCamera);
                var verticalDirection = player.GetVerticalInput();

                // Set this property so that we have a comparison value when needed
                Get<Data>().LastVelocity = player.Velocity;

                var velocity = ComputeVelocity(player.Velocity, moveDirection, verticalDirection, settings, deltaTime);
                Output(new Output.VelocityComputed(velocity));

                var rotation = AlignDroneNoseWithCamera(playerCamera, player.GlobalRotation, settings.RotationSpeed, deltaTime);
                Output(new Output.RotationComputed(rotation));
                
                return ToSelf();
            }

            /// <summary>
            /// Given the current player velocity and the user's input in this frame, compute which value the player
            /// drone becomes in this frame.
            /// </summary>
            /// <param name="velocity"></param>
            /// <param name="moveDirection"></param>
            /// <param name="verticalDirection"></param>
            /// <param name="settings"></param>
            /// <param name="deltaTime"></param>
            /// <returns></returns>
            private static Vector3 ComputeVelocity(Vector3 velocity, Vector3 moveDirection, float verticalDirection, PlayerSettings settings, float deltaTime)
            {
                if (moveDirection != Vector3.Zero)
                {
                    // Accelerate the horizontal velocity until max speed
                    moveDirection *= settings.MaxSpeed;
                    velocity.X = Mathf.MoveToward(velocity.X, moveDirection.X, settings.Acceleration * deltaTime);
                    velocity.Z = Mathf.MoveToward(velocity.Z, moveDirection.Z, settings.Acceleration * deltaTime);
                }
                else
                {
                    // Decelerate the horizontal velocity until zero
                    velocity.X = Mathf.MoveToward(velocity.X, 0f, settings.Acceleration * deltaTime);
                    velocity.Z = Mathf.MoveToward(velocity.Z, 0f, settings.Acceleration * deltaTime);
                }

                if (Mathf.IsEqualApprox(verticalDirection, 0f))
                {
                    // Decelerate the vertical velocity until zero
                    velocity.Y = Mathf.MoveToward(velocity.Y, 0f, settings.VerticalAcceleration * deltaTime);
                }
                else
                {
                    // Accelerate the vertical velocity until max speed
                    verticalDirection *= settings.MaxVerticalSpeed;
                    velocity.Y = Mathf.MoveToward(velocity.Y, verticalDirection, settings.VerticalAcceleration * deltaTime);
                }

                return velocity;
            }
            
            /// <summary>
            /// Given the current player' rotation and main camera's rotation (both in global space), compute the value
            /// that results in a smooth rotation to have the player drone's nose aligned with camera.
            /// </summary>
            /// <param name="camera"></param>
            /// <param name="currentRotation"></param>
            /// <param name="rotationSpeed"></param>
            /// <param name="deltaTime"></param>
            /// <returns></returns>
            private static Vector3 AlignDroneNoseWithCamera(Camera3D camera, Vector3 currentRotation, float rotationSpeed, float deltaTime)
            {
                var targetRotationY = camera.GlobalRotation.Y;
                return currentRotation with
                {
                    Y = Mathf.RotateToward(currentRotation.Y, targetRotationY, rotationSpeed * deltaTime)
                };
            }
        }
    }
}
