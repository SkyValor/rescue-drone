namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IEnemyAILogic : ILogicBlock<EnemyAILogic.State>;

[Meta, LogicBlock(typeof(State), Diagram = true)]
public partial class EnemyAILogic : LogicBlock<EnemyAILogic.State>, IEnemyAILogic
{
    public override Transition GetInitialState() => To<State.Disabled>();
}
