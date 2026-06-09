using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Wheel is idle and ready. Accepts SPIN always, and LEAVE only on safe/super zones
    /// (R7). The leave guard is defensive — the View should already disable the button.
    /// </summary>
    public sealed class IdleState : GameState, ISpinInput
    {
        public IdleState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter() => Ctx.Events.RaisePhaseChanged(GamePhase.Idle);

        public void OnSpin() => Fsm.Change(new SpinningState(Ctx, Fsm));

        public void OnLeave()
        {
            if (ZoneRules.CanLeave(Ctx.Zone, Ctx.Tuning.safeInterval, Ctx.Tuning.superInterval))
                Fsm.Change(new CashOutState(Ctx, Fsm));
        }
    }
}
