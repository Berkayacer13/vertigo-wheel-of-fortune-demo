using System.Collections.Generic;

namespace Wof.Domain
{
    /// <summary>
    /// Holds the rewards banked during the current run. They accumulate as the player
    /// wins (R4), are wiped when a bomb explodes (R8), and are handed out to permanent
    /// inventory when the player cashes out (R10).
    /// </summary>
    public sealed class RewardWallet
    {
        private readonly List<Reward> _runRewards = new List<Reward>();

        public IReadOnlyList<Reward> RunRewards => _runRewards;
        public bool HasRewards => _runRewards.Count > 0;

        /// <summary>(R4) Accumulate a freshly won reward.</summary>
        public void Add(Reward reward) => _runRewards.Add(reward);

        /// <summary>(R8) Bomb: lose everything collected this run.</summary>
        public void DetonateBomb() => _runRewards.Clear();

        public bool TryConsume(RewardKind kind)
        {
            for (int i = 0; i < _runRewards.Count; i++)
            {
                if (_runRewards[i].Kind != kind) continue;
                _runRewards.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// (R10) Returns the banked rewards and clears the run. The caller is
        /// responsible for moving them into permanent inventory/currency.
        /// </summary>
        public IReadOnlyList<Reward> CashOut()
        {
            var banked = new List<Reward>(_runRewards);
            _runRewards.Clear();
            return banked;
        }
    }
}
