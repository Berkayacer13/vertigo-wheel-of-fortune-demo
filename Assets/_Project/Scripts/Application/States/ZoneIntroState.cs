using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Sets up the requested zone (resolves type, rebuilds the wheel, fires events the
    /// strip/HUD react to) and drops the player into Idle. This is the single owner of
    /// zone setup, so other states just request a zone number.
    /// </summary>
    public sealed class ZoneIntroState : GameState
    {
        private readonly int _zone;

        public ZoneIntroState(GameContext ctx, GameStateMachine fsm, int zone) : base(ctx, fsm)
            => _zone = zone;

        public override void Enter()
        {
            Ctx.Events.RaisePhaseChanged(GamePhase.ZoneIntro);
            Ctx.SetZone(_zone);
            Fsm.Change(new IdleState(Ctx, Fsm));
        }
    }
}
