namespace Wof.Domain
{
    /// <summary>
    /// Everything the screen needs to know about the zone the player just entered, resolved
    /// once by the logic layer and carried on the zone-changed event. Without it the
    /// presentation layer had to re-derive "may the player leave?" from <see cref="ZoneRules"/>
    /// and the tuning asset itself, so the rule was evaluated in two layers and the UI needed
    /// game config just to grey out a button.
    /// </summary>
    public readonly struct ZoneInfo
    {
        public readonly int Zone;
        public readonly ZoneType Type;

        /// <summary>Safe/super zone: the player may walk away with the run (R7).</summary>
        public readonly bool CanLeave;

        /// <summary>Zones left to clear before walking away is allowed; 0 when <see cref="CanLeave"/>.</summary>
        public readonly int ZonesUntilLeave;

        // the intervals this zone was resolved with, so neighbouring zones are typed the same way
        private readonly int _safe;
        private readonly int _super;

        // private: the fields are only consistent when derived together from the rules
        private ZoneInfo(int zone, ZoneType type, bool canLeave, int zonesUntilLeave, int safe, int super)
        {
            Zone = zone;
            Type = type;
            CanLeave = canLeave;
            ZonesUntilLeave = zonesUntilLeave;
            _safe = safe;
            _super = super;
        }

        public static ZoneInfo For(int zone,
            int safe = ZoneRules.DefaultSafeInterval, int super = ZoneRules.DefaultSuperInterval)
            => new ZoneInfo(zone,
                ZoneRules.Resolve(zone, safe, super),
                ZoneRules.CanLeave(zone, safe, super),
                ZoneRules.ZonesUntilLeave(zone, safe, super),
                safe, super);

        /// <summary>
        /// The type of any other zone under the same rules — for drawing the zones around this
        /// one without the screen keeping its own copy of the intervals.
        /// </summary>
        public ZoneType TypeAt(int zone) => ZoneRules.Resolve(zone, _safe, _super);
    }
}
