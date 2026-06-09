using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Player walked away on a safe/super zone (R10). Banks the run rewards, broadcasts
    /// the collected list for the cash-out screen, and ends the run on confirm.
    /// </summary>
    public sealed class CashOutState : GameState, ICashOutInput
    {
        public CashOutState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.CashOut);
            var banked = Ctx.Economy.Bank();
            Ctx.Events.RaiseRewardsBanked(banked);
        }

        public void OnConfirm() => Fsm.Change(new GameOverState(Ctx, Fsm));
    }
}
