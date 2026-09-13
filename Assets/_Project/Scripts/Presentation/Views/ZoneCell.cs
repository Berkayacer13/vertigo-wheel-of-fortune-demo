using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>
    /// One chip on the top zone-track. Shows its zone number, wears a coloured plate when it
    /// is a milestone (safe/super) zone, and lifts out of the row when it is the active one.
    /// All of its geometry is handed down by <see cref="ZoneTrackView"/> so the row can size
    /// itself to the screen.
    /// </summary>
    public sealed class ZoneCell : UiView
    {
        [SerializeField] private Image plate;       // ui_image_zone_cell_plate (milestone chip)
        [SerializeField] private Image highlight;   // ui_image_zone_cell_highlight
        [SerializeField] private TMP_Text label;    // ui_text_zone_cell_value

        /// <summary>The active chip stands proud of the row rather than just changing colour.</summary>
        private const float CurrentScale = 1.14f;

        private bool _wasCurrent;

        /// <param name="accent">Zone-type colour: drives the number and the active fill.</param>
        /// <param name="milestone">Safe or super — the zones the player may walk away on.</param>
        public void Set(int zone, Color accent, bool milestone, bool current)
        {
            if (label != null)
            {
                label.text = zone.ToString();
                label.color = current ? Color.white : accent;
            }

            if (plate != null)
            {
                // Milestones carry a coloured chip even when they are still ahead, so the
                // player can see the next safe zone coming up the track instead of having to
                // notice that one number out of seven is tinted green.
                plate.enabled = milestone;
                if (milestone) plate.color = UiPalette.Dim(accent, current ? 0.55f : 0.26f);
            }

            if (highlight != null)
            {
                highlight.enabled = current;
                // Tint the fill with the zone's own colour. The highlight sprite is neutral,
                // so without this every current zone looked the same — and because the label
                // goes white when current, the safe/super colour vanished on the one zone
                // where the player has to decide whether to cash out.
                if (current) highlight.color = accent;
            }

            transform.localScale = Vector3.one * (current ? CurrentScale : 1f);
            if (current && !_wasCurrent) Pop();
            _wasCurrent = current;
        }

        /// <summary>Geometry comes from the track, which sizes the whole row to the screen.</summary>
        public void SetGeometry(Vector2 position, float size, float fontSize)
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            if (plate != null) plate.rectTransform.sizeDelta = new Vector2(size, size);
            if (highlight != null) highlight.rectTransform.sizeDelta = new Vector2(size, size);
            if (label != null) label.fontSize = fontSize;
        }

        /// <summary>Advancing a zone is progress — the chip that takes over says so.</summary>
        private void Pop()
        {
            transform.DOKill();
            transform.localScale = Vector3.one * CurrentScale;
            transform.DOPunchScale(Vector3.one * 0.22f, 0.4f, 8, 0.9f)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one * (_wasCurrent ? CurrentScale : 1f);
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref plate, "ui_image_zone_cell_plate");
            Bind(ref highlight, "ui_image_zone_cell_highlight");
            Bind(ref label, "ui_text_zone_cell_value");
        }
#endif
    }
}
