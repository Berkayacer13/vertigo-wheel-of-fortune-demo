using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// Which phases leave no modal screen up. Every presenter that owns an overlay hides its
    /// own screen, so this rule is shared rather than repeated — otherwise the stash and the
    /// reward popup could disagree about when the wheel is supposed to be uncovered.
    /// </summary>
    internal static class OverlayPolicy
    {
        /// <summary>
        /// Reward, BombExploded and CashOut are absent on purpose: those screens are opened by
        /// their data events, and clearing overlays on their phase change would close them
        /// again the moment they appeared.
        /// </summary>
        public static bool ClearsOverlays(GamePhase phase) =>
            phase == GamePhase.Boot
            || phase == GamePhase.ZoneIntro
            || phase == GamePhase.Idle
            || phase == GamePhase.Spinning
            || phase == GamePhase.Resolving
            || phase == GamePhase.GameOver;
    }
}
