using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Bomb hit. The wallet is NOT wiped yet, so a revive (gold R9, or ad) returns to
    /// Idle with rewards intact. Giving up wipes the run (R8) and ends the game.
    /// </summary>
    public sealed class BombExplodedState : GameState, IReviveInput
    {
        public BombExplodedState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.BombExploded);
            Ctx.Events.RaiseBombExploded();
        }

        public void OnReviveGold()
        {
            if (!Ctx.Economy.TrySpendGold(Ctx.Settings.reviveGoldCost)) return;
            Resume(); // rewards kept (R9)
        }

        public void OnReviveAd() => Resume(); // ad reward assumed granted

        /// <summary>
        /// Spend a shield instead of gold or an ad. Returns false to the View when the
        /// wallet holds none, so the button can fall back rather than dying silently.
        /// </summary>
        public bool OnReviveShield()
        {
            if (!Ctx.Economy.TryConsumeShield()) return false;
            Resume(); // rewards kept, minus the shield
            return true;
        }

        /// <summary>
        /// Back to the wheel with the run intact. The chambers are re-ordered on the way out
        /// because this is the only path that spins the same wheel twice — without it the
        /// player would already know where the bomb is on the spin they just paid to retry.
        /// It happens while the bomb screen still covers the wheel, so the swap is unseen.
        /// </summary>
        private void Resume()
        {
            Ctx.ShuffleCurrentWheel();
            Fsm.Change(new IdleState(Ctx, Fsm));
        }

        public void OnGiveUp()
        {
            Ctx.Economy.Wipe(); // (R8) lose everything
            Fsm.Change(new GameOverState(Ctx, Fsm));
        }
    }
}
