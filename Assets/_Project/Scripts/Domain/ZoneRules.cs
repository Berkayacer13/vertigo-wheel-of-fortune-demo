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

        /// <summary>
        /// How many more zones the player must clear before walking away is allowed;
        /// 0 when the current zone already allows it. The UI uses this to say *why* the
        /// LEAVE button is dark ("SAFE ZONE IN 3") instead of just greying it out — the
        /// every-5th rule is invisible to a first-time player otherwise.
        /// </summary>
        public static int ZonesUntilLeave(int zone,
            int safe = DefaultSafeInterval, int super = DefaultSuperInterval)
        {
            int from = zone > 0 ? zone : 0;
            if (from > 0 && CanLeave(from, safe, super)) return 0;

            // Super is not always a multiple of Safe once a designer retunes the intervals,
            // so take whichever milestone lands first rather than assuming it is Safe.
            int next = System.Math.Min(NextMultipleAfter(from, safe), NextMultipleAfter(from, super));
            return next == int.MaxValue ? 0 : next - from;
        }

        /// <summary>Smallest multiple of <paramref name="interval"/> strictly above <paramref name="value"/>.</summary>
        private static int NextMultipleAfter(int value, int interval)
            => interval <= 0 ? int.MaxValue : (value / interval + 1) * interval;

        public static WheelTier TierFor(ZoneType type) => type switch
        {
            ZoneType.Super => WheelTier.Golden,
            ZoneType.Safe => WheelTier.Silver,
            _ => WheelTier.Bronze,
        };
    }
}
