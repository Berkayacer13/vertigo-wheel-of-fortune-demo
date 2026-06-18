using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Reward popup is showing. The reward is already in the run wallet, so Collect just
    /// advances to the next zone. On a safe/super zone the player may instead cash out
    /// right here (R7) — keeping the just-won silver/golden reward — via OnCollectAndLeave.
    /// </summary>
    public sealed class RewardState : GameState, ICollectInput
    {
        public RewardState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter() => Ctx.Events.RaisePhaseChanged(GamePhase.Reward);

        public void OnCollect() => Fsm.Change(new ZoneIntroState(Ctx, Fsm, Ctx.Zone + 1));

        public void OnCollectAndLeave()
        {
            if (ZoneRules.CanLeave(Ctx.Zone, Ctx.Tuning.safeInterval, Ctx.Tuning.superInterval))
                Fsm.Change(new CashOutState(Ctx, Fsm));
        }
    }
}
