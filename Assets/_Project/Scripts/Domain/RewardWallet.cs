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

        /// <summary>
        /// Removes the first reward of <paramref name="kind"/> and hands it back, so the
        /// caller can report the reward's real Id instead of guessing a content string.
        /// </summary>
        public bool TryConsume(RewardKind kind, out Reward consumed)
        {
            for (int i = 0; i < _runRewards.Count; i++)
            {
                if (_runRewards[i].Kind != kind) continue;
                consumed = _runRewards[i];
                _runRewards.RemoveAt(i);
                return true;
            }

            consumed = default;
            return false;
        }

        public bool TryConsume(RewardKind kind) => TryConsume(kind, out _);

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
