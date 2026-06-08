using System.Collections.Generic;

namespace Wof.Domain
{
    /// <summary>
    /// Strategy for choosing which slice the wheel lands on. Pulling this behind an
    /// interface lets the spin be deterministic in tests and swappable at runtime.
    /// </summary>
    public interface ISliceSelector
    {
        int PickLandingIndex(IReadOnlyList<float> weights);
    }
}
