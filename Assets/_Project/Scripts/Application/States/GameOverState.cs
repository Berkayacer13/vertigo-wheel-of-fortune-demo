using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// End of run (after a bomb give-up or a cash-out). Restart wipes any leftover run
    /// rewards and starts again from zone 1 (R8).
    /// </summary>
    public sealed class GameOverState : GameState, IRestartInput
    {
        public GameOverState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter() => Ctx.Events.RaisePhaseChanged(GamePhase.GameOver);

        public void OnRestart()
        {
            Ctx.Economy.Wipe();
            Fsm.Change(new ZoneIntroState(Ctx, Fsm, zone: 1));
        }
    }
}
