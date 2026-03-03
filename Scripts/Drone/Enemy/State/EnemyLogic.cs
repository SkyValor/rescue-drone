namespace RescueDrone;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IEnemyLogic : ILogicBlock<EnemyLogic.State> { }

[Meta, LogicBlock(typeof(State))]
public partial class EnemyLogic : LogicBlock<EnemyLogic.State>, IEnemyLogic
{
	public override Transition GetInitialState() => To<State.Idle>();
}
