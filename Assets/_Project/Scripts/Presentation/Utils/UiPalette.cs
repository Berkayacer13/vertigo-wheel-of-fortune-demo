using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Colours that mean the same thing in more than one place. The zone gold, the safe-zone
    /// green and the neutral zone grey were re-typed as literals in each view (gold in four
    /// places), so retuning one left the others silently out of step. A colour used by a
    /// single screen stays local to that screen.
    /// </summary>
    public static class UiPalette
    {
        /// <summary>Super zones and the headline gold of titles.</summary>
        public static readonly Color Gold = new Color(1f, 0.78f, 0.18f);

        /// <summary>Safe zones, and "this spin is free".</summary>
        public static readonly Color SafeGreen = new Color(0.45f, 0.92f, 0.36f);

        /// <summary>An ordinary zone number.</summary>
        public static readonly Color ZoneNeutral = new Color(0.82f, 0.84f, 0.88f);

        /// <summary>Darken a colour without making it see-through (Color * float scales alpha too).</summary>
        public static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
