using System;
using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class WheelModelTests
    {
        [Test]
        public void Shuffle_preserves_slices_weights_and_count()
        {
            var slices = new[]
            {
                new WheelSlice(new Reward("gold", RewardKind.Gold, 1u, "gold"), 2f),
                new WheelSlice(new Reward("shield", RewardKind.Shield, 1u, "shield"), 0.6f),
                new WheelSlice(new Reward("bomb", RewardKind.Bomb, 0u, "bomb"), 1f),
            };
            var wheel = new WheelModel(WheelTier.Bronze, slices);

            var shuffled = wheel.Shuffled(new Random(4));

            Assert.AreEqual(wheel.SliceCount, shuffled.SliceCount);
            float originalWeight = 0f;
            float shuffledWeight = 0f;
            foreach (var slice in wheel.Slices) originalWeight += slice.Weight;
            foreach (var slice in shuffled.Slices) shuffledWeight += slice.Weight;
            Assert.AreEqual(originalWeight, shuffledWeight);
            CollectionAssert.AreEquivalent(
                new[] { "gold", "shield", "bomb" },
                new[] { shuffled.SliceAt(0).Reward.Id, shuffled.SliceAt(1).Reward.Id, shuffled.SliceAt(2).Reward.Id });
        }
    }
}