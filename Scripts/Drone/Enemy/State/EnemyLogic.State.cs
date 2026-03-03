namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class EnemyLogic
{
    [Meta]
    public partial record State : StateLogic<State>;
}