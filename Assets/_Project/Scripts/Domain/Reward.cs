namespace Wof.Domain
{
    /// <summary>
    /// An immutable payout result the wallet stores. Deliberately decoupled from the
    /// ScriptableObject so all game logic is testable without any Unity asset.
    /// </summary>
    public readonly struct Reward
    {
        public readonly string Id;
        public readonly RewardKind Kind;
        public readonly int Amount;

        /// <summary>Sprite lookup key (asset name), not a Sprite — keeps the Domain UI-free.</summary>
        public readonly string IconKey;

        public Reward(string id, RewardKind kind, int amount, string iconKey)
        {
            Id = id;
            Kind = kind;
            Amount = amount;
            IconKey = iconKey;
        }

        public bool IsBomb => Kind == RewardKind.Bomb;

        /// <summary>Returns a copy with a new amount (used by the reward scaler).</summary>
        public Reward WithAmount(int amount) => new Reward(Id, Kind, amount, IconKey);
    }
}
