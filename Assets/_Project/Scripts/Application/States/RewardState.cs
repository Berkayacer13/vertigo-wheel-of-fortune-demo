using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Reward popup is showing. Waits for the player to tap Collect, then advances to the
    /// next zone.
    /// </summary>
    public sealed class RewardState : GameState, ICollectInput
    {
        public RewardState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override void Enter() => Ctx.Events.RaisePhaseChanged(GamePhase.Reward);

        public void OnCollect() => Fsm.Change(new ZoneIntroState(Ctx, Fsm, Ctx.Zone + 1));
    }
}
