namespace Wof.Domain
{
    /// <summary>
    /// Which rewards a wheel tier may carry. Kept as a named rule because the shield has
    /// already moved between wheels twice in this project's history; the bootstrap, the wheel
    /// inspector and the tests all ask this one place instead of each remembering it.
    /// </summary>
    public static class RewardRules
    {
        /// <summary>
        /// The shield is the silver spin's special gift: it appears on the silver (safe-zone)
        /// wheel and nowhere else. Every other reward is unrestricted by tier.
        /// </summary>
        public static bool CanAppearOn(RewardKind kind, WheelTier tier)
            => kind != RewardKind.Shield || tier == WheelTier.Silver;
    }
}
