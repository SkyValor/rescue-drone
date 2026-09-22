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
                if (!Get<IGameRepo>().PlayerInControl.Value) return ToSelf();
                
                if (input.Event.IsActionPressed(GameInputs.KbToggleMouseCapture))
                    Output(new Output.ToggleMouseCapture());
                        
                return ToSelf();
            }

            public Transition On(in Input.OnPhysicsTick input)
            {
                var settings = Get<PlayerSettings>();
                var gameRepo = Get<IGameRepo>();
                var player = Get<PlayerDrone>();

                var deltaTime = (float) input.Delta;
                var deviceHandler = gameRepo.InputDeviceHandler.Value;
                if (deviceHandler is null) return ToSelf();
                
                var inputComponent = deviceHandler.CurrentInputComponent;
                inputComponent.PhysicsMovementUpdate();
                
                // Get the user's horizontal movement input and output it to fuel the tilting feature
                var horizontalInputDirection = inputComponent.HorizontalInput;
                Output(new Output.MoveDirectionTilt(horizontalInputDirection, input.Delta));

                // Get this input direction based on the player drone's nose
                var playerCamera = gameRepo.MainCamera.Value;
                var moveDirection = GetInputBasedOnCamera(horizontalInputDirection, playerCamera);
                var verticalInputDirection = inputComponent.VerticalInput;

                // Cache this value for a later comparison
                Get<Data>().LastVelocity = player.Velocity;
                
                var velocity = ComputeVelocity(player.Velocity, moveDirection, verticalInputDirection, settings, deltaTime);
                Output(new Output.VelocityComputed(velocity));
                
                var rotation = AlignDroneNoseWithCamera(playerCamera, player.GlobalRotation, settings.RotationSpeed, deltaTime);
                Output(new Output.RotationComputed(rotation));
                
                return ToSelf();
            }

            private static Vector3 GetInputBasedOnCamera(Vector2 inputDirection, Camera3D camera)
            {
                if (camera is null)
                {
                    GD.PrintErr("Parameter 'camera' is null. Player drone cannot get input based on camera.");
                    return Vector3.Zero;
                }

                var cameraBasis = camera.Basis;
                var input = new Vector3
                {
                    X = inputDirection.X,
                    Z = inputDirection.Y,
                };

                return cameraBasis * input with { Y = 0f };
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
                    velocity.X = Mathf.MoveToward(velocity.X, 0f, settings.Deceleration * deltaTime);
                    velocity.Z = Mathf.MoveToward(velocity.Z, 0f, settings.Deceleration * deltaTime);
                }

                if (Mathf.IsEqualApprox(verticalDirection, 0f))
                {
                    // Decelerate the vertical velocity until zero
                    velocity.Y = Mathf.MoveToward(velocity.Y, 0f, settings.VerticalDeceleration * deltaTime);
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
