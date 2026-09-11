using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Reads the landed slice and forks: bomb -> BombExploded (wallet kept for a possible
    /// revive), otherwise scale + bank the reward and show the popup.
    /// </summary>
    public sealed class ResolvingState : GameState
    {
        private readonly int _index;

        public ResolvingState(GameContext ctx, GameStateMachine fsm, int index) : base(ctx, fsm)
            => _index = index;

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.Resolving);
            var slice = Ctx.CurrentWheel.SliceAt(_index);

            // NOTE: the chambers are deliberately NOT re-ordered here. Every reward path
            // ends in ZoneIntro, which builds and shuffles a fresh wheel anyway, and the
            // views now hold the wheel uncovered for a beat so the player can see what it
            // landed on — re-ordering at this instant would swap the icons in front of them.
            // The one path that re-spins the same wheel (bomb -> revive) shuffles itself.

            if (slice.IsBomb)
            {
                Fsm.Change(new BombExplodedState(Ctx, Fsm));
            }
            else
            {
                var scaled = Ctx.Scaler.Scale(slice.Reward, Ctx.Zone);
                Ctx.Economy.AddRunReward(scaled);
                Ctx.Events.RaiseRewardWon(scaled);
                Fsm.Change(new RewardState(Ctx, Fsm));
            }
        }
    }
}
