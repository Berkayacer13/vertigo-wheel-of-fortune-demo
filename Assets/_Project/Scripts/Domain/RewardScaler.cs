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
            int scaled = RoundHalfAwayFromZero(baseReward.Amount * _multiplier(zone));
            return baseReward.WithAmount(scaled);
        }

        private static int RoundHalfAwayFromZero(float v)
            => (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }
}
