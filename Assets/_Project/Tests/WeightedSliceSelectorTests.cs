using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class WeightedSliceSelectorTests
    {
        [Test]
        public void Never_picks_a_zero_weight_and_respects_ratio()
        {
            var sel = new WeightedSliceSelector(seed: 42);
            var weights = new[] { 1f, 0f, 3f };   // index 1 must NEVER win; index 2 ~3x index 0
            var hits = new int[3];

            for (int i = 0; i < 100_000; i++)
                hits[sel.PickLandingIndex(weights)]++;

            Assert.AreEqual(0, hits[1], "zero-weight slice should never be picked");
            Assert.That(hits[2], Is.GreaterThan(hits[0]), "weight 3 should win more than weight 1");

            // ratio should be roughly 3:1 (allow generous tolerance for RNG)
            double ratio = (double)hits[2] / hits[0];
            Assert.That(ratio, Is.InRange(2.6, 3.4));
        }

        [Test]
        public void Same_seed_is_deterministic()
        {
            var weights = new[] { 2f, 1f, 1f, 4f };
            var a = new WeightedSliceSelector(seed: 7);
            var b = new WeightedSliceSelector(seed: 7);

            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.PickLandingIndex(weights), b.PickLandingIndex(weights));
        }

        [Test]
        public void All_zero_weights_fall_back_to_uniform_range()
        {
            var sel = new WeightedSliceSelector(seed: 1);
            var weights = new[] { 0f, 0f, 0f };
            for (int i = 0; i < 100; i++)
                Assert.That(sel.PickLandingIndex(weights), Is.InRange(0, 2));
        }
    }
}
