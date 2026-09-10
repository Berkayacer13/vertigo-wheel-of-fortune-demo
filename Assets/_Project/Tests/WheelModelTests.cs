using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class WheelModelTests
    {
        private static WheelModel NewWheel() => new WheelModel(WheelTier.Bronze, new[]
        {
            new WheelSlice(new Reward("gold", RewardKind.Gold, 1u, "gold"), 2f),
            new WheelSlice(new Reward("shield", RewardKind.Shield, 1u, "shield"), 0.6f),
            new WheelSlice(new Reward("bomb", RewardKind.Bomb, 0u, "bomb"), 1f),
        });

        private static Dictionary<string, float> WeightById(WheelModel wheel)
        {
            var map = new Dictionary<string, float>();
            foreach (var slice in wheel.Slices) map[slice.Reward.Id] = slice.Weight;
            return map;
        }

        private static string[] Order(WheelModel wheel)
        {
            var ids = new string[wheel.SliceCount];
            for (int i = 0; i < wheel.SliceCount; i++) ids[i] = wheel.SliceAt(i).Reward.Id;
            return ids;
        }

        [Test]
        public void Shuffle_keeps_every_reward_bound_to_its_own_weight()
        {
            // Summing the weights is not enough: a shuffle that moved rewards independently
            // of their weights would keep the same total while handing the bomb the gold
            // slice's odds. Each reward must still carry the weight it started with.
            var wheel = NewWheel();
            var before = WeightById(wheel);

            var shuffled = wheel.Shuffled(new Random(4));

            Assert.AreEqual(wheel.SliceCount, shuffled.SliceCount);
            var after = WeightById(shuffled);
            CollectionAssert.AreEquivalent(before.Keys, after.Keys);
            foreach (var pair in before)
                Assert.AreEqual(pair.Value, after[pair.Key], $"'{pair.Key}' changed weight");
        }

        [Test]
        public void Shuffle_actually_reorders_the_chambers()
        {
            var wheel = NewWheel();
            var original = Order(wheel);

            // scan seeds rather than trusting one: a correct shuffle is allowed to return
            // the identity order occasionally, but it must not be stuck on it
            bool reordered = false;
            for (int seed = 0; seed < 50 && !reordered; seed++)
                reordered = !AreEqual(original, Order(wheel.Shuffled(new Random(seed))));

            Assert.IsTrue(reordered, "no seed in 0..49 produced a different order");
        }

        [Test]
        public void Shuffle_can_move_a_slice_to_any_position()
        {
            // guards against a partial shuffle (e.g. a loop that never touches index 0),
            // which would pin a slice under the pointer run after run
            var wheel = NewWheel();
            var seen = new HashSet<int>();

            for (int seed = 0; seed < 200; seed++)
            {
                var order = Order(wheel.Shuffled(new Random(seed)));
                seen.Add(Array.IndexOf(order, "bomb"));
            }

            Assert.AreEqual(wheel.SliceCount, seen.Count, "the bomb never reached every position");
        }

        [Test]
        public void Shuffle_does_not_mutate_the_source_wheel()
        {
            var wheel = NewWheel();
            var before = Order(wheel);

            wheel.Shuffled(new Random(7));

            CollectionAssert.AreEqual(before, Order(wheel), "Shuffled must return a new wheel");
        }

        [Test]
        public void Shuffle_is_deterministic_for_a_given_seed()
        {
            // the seedable overload is what lets a test pin a spin result, so the same
            // seed must always produce the same chamber order
            var wheel = NewWheel();

            var first = Order(wheel.Shuffled(new Random(11)));
            var second = Order(wheel.Shuffled(new Random(11)));

            CollectionAssert.AreEqual(first, second);
        }

        private static bool AreEqual(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
