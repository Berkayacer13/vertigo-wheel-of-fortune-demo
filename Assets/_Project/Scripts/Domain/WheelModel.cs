using System.Collections.Generic;

namespace Wof.Domain
{
    /// <summary>
    /// The runtime wheel for a given zone: an ordered set of slices plus the geometry
    /// helpers the View needs. Built from a <c>WheelConfig</c> by the WheelBuilder.
    /// </summary>
    public sealed class WheelModel
    {
        public WheelTier Tier { get; }
        public IReadOnlyList<WheelSlice> Slices { get; }
        public int SliceCount => Slices.Count;

        public WheelModel(WheelTier tier, IReadOnlyList<WheelSlice> slices)
        {
            Tier = tier;
            Slices = slices;
        }

        /// <summary>Angular size of one chamber in degrees.</summary>
        public float SliceAngle => 360f / SliceCount;

        public WheelSlice SliceAt(int index) => Slices[index];

        /// <summary>Per-slice weights, ready to hand to an <see cref="ISliceSelector"/>.</summary>
        public IReadOnlyList<float> Weights()
        {
            var w = new float[SliceCount];
            for (int i = 0; i < SliceCount; i++) w[i] = Slices[i].Weight;
            return w;
        }
    }
}
