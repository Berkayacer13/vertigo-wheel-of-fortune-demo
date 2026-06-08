using System;
using System.Collections.Generic;

namespace Wof.Domain
{
    /// <summary>
    /// Weighted random landing selector. Injecting a <paramref name="seed"/> makes the
    /// distribution unit-testable (run many rolls, assert frequencies match weights).
    /// The View never touches RNG — it only animates to the index chosen here.
    /// </summary>
    public sealed class WeightedSliceSelector : ISliceSelector
    {
        private readonly Random _rng;

        public WeightedSliceSelector(int? seed = null)
            => _rng = seed.HasValue ? new Random(seed.Value) : new Random();

        public int PickLandingIndex(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return _rng.Next(weights.Count); // all-zero guard -> uniform

            double roll = _rng.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Math.Max(0f, weights[i]);
                if (roll <= 0d) return i;
            }
            return weights.Count - 1;
        }
    }
}
