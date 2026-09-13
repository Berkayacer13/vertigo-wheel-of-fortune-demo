using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Bomb hit. The wallet is NOT wiped yet, so a revive (gold R9, a held shield, or ad) returns to
    /// Idle with rewards intact. Giving up wipes the run (R8) and ends the game.
    /// </summary>
    public sealed class BombExplodedState : GameState, IReviveInput
    {
        public BombExplodedState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.BombExploded);
            // the revive offer travels with the event, so the screen never reaches back into
            // the economy or the settings asset to work out what to show
            Ctx.Events.RaiseBombExploded(Ctx.Settings.reviveGoldCost, Ctx.Economy.ShieldCount);
        }

        public void OnReviveGold()
        {
            if (!Ctx.Economy.TrySpendGold(Ctx.Settings.reviveGoldCost)) return;
            Resume(); // rewards kept (R9)
        }

        /// <summary>
        /// Revive without spending gold. A held shield is the better offer, so it is spent
        /// first; with none left, the rewarded ad pays (assumed granted in this demo). This
        /// used to be decided in the bomb screen's click handler, which left the view choosing
        /// what the player paid with.
        /// </summary>
        public void OnReviveFree()
        {
            Ctx.Economy.TryConsumeShield(); // false, and a no-op, when the wallet holds none
            Resume(); // rewards kept, minus the shield if one was spent
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
