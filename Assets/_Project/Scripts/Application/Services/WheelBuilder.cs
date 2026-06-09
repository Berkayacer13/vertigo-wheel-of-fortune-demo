using System.Collections.Generic;
using Wof.Data;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Factory that assembles a runtime <see cref="WheelModel"/> for a given zone:
    /// resolves the zone type, picks the matching wheel config (bronze/silver/golden)
    /// and converts its slice entries into domain slices.
    /// </summary>
    public sealed class WheelBuilder
    {
        private readonly ZoneTuning _tuning;

        public WheelBuilder(ZoneTuning tuning) => _tuning = tuning;

        public WheelModel BuildForZone(int zone)
        {
            var type = ZoneRules.Resolve(zone, _tuning.safeInterval, _tuning.superInterval);
            WheelConfig cfg = type switch
            {
                ZoneType.Super => _tuning.superWheel,
                ZoneType.Safe => _tuning.safeWheel,
                _ => _tuning.normalWheel,
            };

            var slices = new List<WheelSlice>(cfg.SliceCount);
            foreach (var e in cfg.Slices)
            {
                Reward r = e.isBomb
                    ? new Reward("bomb", RewardKind.Bomb, 0, "ui_card_icon_death")
                    : e.reward.ToReward(e.reward.BaseAmount);
                slices.Add(new WheelSlice(r, e.weight));
            }

            return new WheelModel(ZoneRules.TierFor(type), slices);
        }
    }
}
