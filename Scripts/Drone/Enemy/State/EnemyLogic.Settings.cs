namespace RescueDrone;

public partial class EnemyLogic
{
    public record Settings(
        float SpringStrength,
        float Damping,
        float MaxSpeed);
}
