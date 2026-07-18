namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IEnemyAILogic : ILogicBlock<EnemyAILogic.State>;

[Meta, LogicBlock(typeof(State))]
public partial class EnemyAILogic : LogicBlock<EnemyAILogic.State>, IEnemyAILogic
{
    public override Transition GetInitialState() => To<State.Idle>();
}
