using System.Collections.Generic;

namespace Wof.Domain
{
    /// <summary>Rewards owned permanently after a successful cash-out.</summary>
    public sealed class PermanentRewardInventory
    {
        private readonly Dictionary<string, Reward> _rewards = new Dictionary<string, Reward>();

        public IReadOnlyCollection<Reward> Rewards => _rewards.Values;
        public int Count => _rewards.Count;

        public void Add(Reward reward)
        {
            if (_rewards.TryGetValue(reward.Id, out var existing))
            {
                _rewards[reward.Id] = existing.WithAmount(existing.Amount + reward.Amount);
                return;
            }

            _rewards.Add(reward.Id, reward);
        }

        public bool TryGet(string rewardId, out Reward reward) => _rewards.TryGetValue(rewardId, out reward);

        public void Clear() => _rewards.Clear();
    }
}