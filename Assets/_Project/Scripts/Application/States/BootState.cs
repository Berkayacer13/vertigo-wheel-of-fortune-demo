using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Entry state: broadcasts the starting currency/wallet so the HUD initialises,
    /// then hands off to the first zone.
    /// </summary>
    public sealed class BootState : GameState
    {
        public BootState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.Boot);
            Ctx.Events.RaiseCurrencyChanged(Ctx.Economy.Gold, Ctx.Economy.Cash);
            Ctx.Events.RaiseWalletChanged(0);
            Fsm.Change(new ZoneIntroState(Ctx, Fsm, zone: 1));
        }
    }
}
