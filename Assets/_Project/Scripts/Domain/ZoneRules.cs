namespace Wof.Domain
{
    /// <summary>
    /// Pure zone-type rules (R5/R6/R7). The most-tested file in the project.
    /// Note the order: Super (×30) is checked BEFORE Safe (×5), because 30 is also
    /// divisible by 5 — zone 30/60/90 must resolve to Super, not Safe.
    /// </summary>
    public static class ZoneRules
    {
        public const int DefaultSafeInterval = 5;
        public const int DefaultSuperInterval = 30;

        public static ZoneType Resolve(int zone,
            int safeInterval = DefaultSafeInterval,
            int superInterval = DefaultSuperInterval)
        {
            if (zone <= 0) return ZoneType.Normal;
            if (zone % superInterval == 0) return ZoneType.Super; // check Super FIRST
            if (zone % safeInterval == 0) return ZoneType.Safe;
            return ZoneType.Normal;
        }

        /// <summary>Only Normal zones carry a bomb (R3).</summary>
        public static bool HasBomb(int zone,
            int safe = DefaultSafeInterval, int super = DefaultSuperInterval)
            => Resolve(zone, safe, super) == ZoneType.Normal;

        /// <summary>Player may walk away only on Safe/Super zones (R7).</summary>
        public static bool CanLeave(int zone,
            int safe = DefaultSafeInterval, int super = DefaultSuperInterval)
            => Resolve(zone, safe, super) != ZoneType.Normal;

        public static WheelTier TierFor(ZoneType type) => type switch
        {
            ZoneType.Super => WheelTier.Golden,
            ZoneType.Safe => WheelTier.Silver,
            _ => WheelTier.Bronze,
        };
    }
}
