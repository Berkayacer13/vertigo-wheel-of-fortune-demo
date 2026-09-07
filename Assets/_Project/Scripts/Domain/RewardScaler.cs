using System;

namespace Wof.Domain
{
    /// <summary>
    /// Scales a base reward by a zone-dependent multiplier (R4: rewards "get better
    /// every zone"). The multiplier is injected (typically ZoneTuning.rewardGrowth)
    /// so this stays pure and UI-free. Bombs are never scaled.
    /// </summary>
    public sealed class RewardScaler
    {
        private readonly Func<int, float> _multiplier;

        public RewardScaler(Func<int, float> multiplier) => _multiplier = multiplier;

        public Reward Scale(Reward baseReward, int zone)
        {
            if (baseReward.IsBomb) return baseReward;
            uint scaled = ScaleAmount(baseReward.Amount, _multiplier(zone));
            return baseReward.WithAmount(scaled);
        }

        private static uint ScaleAmount(uint amount, float multiplier)
        {
            double scaled = Math.Max(0d, Math.Round(amount * multiplier, MidpointRounding.AwayFromZero));
            return checked((uint)scaled);
        }
    }
}
