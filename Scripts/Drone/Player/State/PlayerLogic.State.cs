namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class PlayerLogic
{
    [Meta]
    public partial record State : StateLogic<State>;
}
