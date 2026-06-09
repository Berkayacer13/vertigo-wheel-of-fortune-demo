using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// The core moment. Decides the landing index up-front (pure, testable), lets the
    /// View animate the cylinder to that index, then hands off to resolution. The
    /// animation only visualises a result the logic already chose.
    /// </summary>
    public sealed class SpinningState : GameState
    {
        public SpinningState(GameContext ctx, GameStateMachine fsm) : base(ctx, fsm) { }

        public override async void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.Spinning);

            // 1) Decide the result first (deterministic, unit-tested).
            int index = Ctx.Selector.PickLandingIndex(Ctx.CurrentWheel.Weights());

            // 2) Let the View spin the cylinder to that index and await it.
            Ctx.Events.RaiseSpinStarted();
            await Ctx.SpinAnimator(index);
            Ctx.Events.RaiseSpinLanded(index);

            // 3) Resolve reward vs bomb.
            Fsm.Change(new ResolvingState(Ctx, Fsm, index));
        }
    }
}
