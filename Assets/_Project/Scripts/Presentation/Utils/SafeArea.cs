using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Re-anchors a RectTransform to Screen.safeArea so the UI clears notches/cutouts on
    /// the 20:9 / 16:9 / 4:3 targets. Put this on a child of the root canvas, never the
    /// root itself.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastApplied;
        private Vector2Int _lastResolution;

        private void Awake() => _rect = GetComponent<RectTransform>();

        private void Update()
        {
            if (ShouldReapply()) Apply();
        }

        private bool ShouldReapply()
        {
            return Screen.safeArea != _lastApplied
                   || _lastResolution.x != Screen.width
                   || _lastResolution.y != Screen.height;
        }

        private void Apply()
        {
            int w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;   // nothing sane to normalise against yet

            var safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= w; min.y /= h;
            max.x /= w; max.y /= h;

            // Clamp: safeArea and Screen.width/height are read from two different places and
            // can disagree for a frame after a resolution change — in the editor a Game-view
            // resize does exactly that. Un-clamped, the division then yields anchors past 1
            // and the whole UI is laid out for a screen that does not exist, which is how a
            // capture came out zoomed and shoved into the bottom-right corner.
            min.x = Mathf.Clamp01(min.x); min.y = Mathf.Clamp01(min.y);
            max.x = Mathf.Clamp01(max.x); max.y = Mathf.Clamp01(max.y);
            if (max.x <= min.x || max.y <= min.y) return;   // degenerate — keep the last good one

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _lastApplied = safe;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
