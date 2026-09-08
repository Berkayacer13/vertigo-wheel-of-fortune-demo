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

            if (Ctx.Economy.TryConsumeShield())
            {
                Fsm.Change(new IdleState(Ctx, Fsm));
                return;
            }

            Ctx.Events.RaiseBombExploded();
        }

        public void OnReviveGold()
        {
            if (Ctx.Economy.TrySpendGold(Ctx.Settings.reviveGoldCost))
                Fsm.Change(new IdleState(Ctx, Fsm)); // rewards kept (R9)
        }

        public void OnReviveAd() => Fsm.Change(new IdleState(Ctx, Fsm)); // ad reward assumed granted

        public void OnGiveUp()
        {
            Ctx.Economy.Wipe(); // (R8) lose everything
            Fsm.Change(new GameOverState(Ctx, Fsm));
        }
    }
}
