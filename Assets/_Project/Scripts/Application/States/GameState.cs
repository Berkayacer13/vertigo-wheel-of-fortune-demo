namespace Wof.Application
{
    /// <summary>Shared base for states: holds the context + state machine and no-op Tick/Exit.</summary>
    public abstract class GameState : IGameState
    {
        protected readonly GameContext Ctx;
        protected readonly GameStateMachine Fsm;

        protected GameState(GameContext ctx, GameStateMachine fsm)
        {
            Ctx = ctx;
            Fsm = fsm;
        }

        public abstract void Enter();
        public virtual void Tick(float dt) { }
        public virtual void Exit() { }
    }
}
