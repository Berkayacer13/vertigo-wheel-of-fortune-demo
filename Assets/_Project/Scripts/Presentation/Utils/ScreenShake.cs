using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Decaying-trauma shake for the UI root. Hits <em>add</em> trauma rather than resetting
    /// it, and the offset is driven by trauma squared, so a small knock barely moves and a
    /// bomb really punches. The displacement is sampled from sine waves instead of fresh
    /// randomness each frame — per-frame random reads as static, not as impact.
    /// <para>
    /// This must sit on a rect of its own, never on the <see cref="SafeArea"/> rect: that one
    /// owns its anchors and offsets and would fight the shake for the same numbers.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenShake : MonoBehaviour
    {
        [Tooltip("Trauma lost per second; the shake always ends on its own.")]
        [SerializeField] private float decay = 1.8f;
        [SerializeField] private Vector2 maxOffset = new Vector2(34f, 24f);
        [SerializeField] private float maxRoll = 1.6f;      // degrees
        [SerializeField] private float frequency = 26f;

        private RectTransform _rect;
        private Vector2 _rest;
        private float _trauma;
        private float _time;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _rest = _rect.anchoredPosition;
        }

        /// <summary>Add impact. Clamped to 1, so stacked hits cannot shake the screen apart.</summary>
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        // LateUpdate so the shake is applied after any layout pass has positioned the UI.
        private void LateUpdate()
        {
            if (_trauma <= 0f) return;

            // unscaled: the shake must still decay if game time is ever slowed for a hit-stop
            _trauma = Mathf.Max(0f, _trauma - decay * Time.unscaledDeltaTime);
            _time += Time.unscaledDeltaTime * frequency;

            float shake = _trauma * _trauma;
            _rect.anchoredPosition = _rest + new Vector2(
                maxOffset.x * shake * Mathf.Sin(_time * 1.7f),
                maxOffset.y * shake * Mathf.Sin(_time * 2.3f));
            _rect.localRotation = Quaternion.Euler(0f, 0f, maxRoll * shake * Mathf.Sin(_time * 1.1f));

            if (_trauma > 0f) return;
            _rect.anchoredPosition = _rest;                 // always return to rest exactly
            _rect.localRotation = Quaternion.identity;
        }
    }
}
