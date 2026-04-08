namespace RescueDrone.Scripts;

using Godot;

public partial class SimpleCharacterController : CharacterBody3D
{
    [Export] public float Speed { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 input = Vector3.Zero;
        
        if (Input.IsActionPressed("move_forward")) input.Z -= 1;
        if (Input.IsActionPressed("move_back")) input.Z += 1;
        if (Input.IsActionPressed("move_left")) input.X -= 1;
        if (Input.IsActionPressed("move_right")) input.X += 1;

        Velocity = input.Normalized() * Speed;
        MoveAndSlide();
    }
}
