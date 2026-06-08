namespace Wof.Domain
{
    /// <summary>One chamber of the runtime wheel: a reward plus its landing weight.</summary>
    public sealed class WheelSlice
    {
        public Reward Reward { get; }
        public float Weight { get; }
        public bool IsBomb => Reward.IsBomb;

        public WheelSlice(Reward reward, float weight)
        {
            Reward = reward;
            Weight = weight;
        }
    }
}
