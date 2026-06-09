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
            var safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _lastApplied = safe;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
