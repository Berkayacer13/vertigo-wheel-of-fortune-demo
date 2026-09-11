using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>One chamber of the wheel: renders a reward icon + amount, or the bomb.</summary>
    public sealed class SliceView : UiView
    {
        [SerializeField] private Image iconValue;        // ui_image_slice_icon_value (decorative -> RaycastTarget OFF)
        [SerializeField] private TMP_Text amountValue;   // ui_text_slice_amount_value
        [SerializeField] private Image amountBg;         // ui_image_slice_amount_bg (pill behind it)
        [SerializeField] private SpriteRegistry sprites; // assigned on the prefab/asset

        /// <summary>Rotor units from a chamber centre down to the badge (hole radius is 70).</summary>
        private const float BadgeDrop = 62f;

        public void Render(WheelSlice slice)
        {
            if (slice == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            ResetPop();   // a re-render must not inherit a half-played landing punch
            iconValue.sprite = sprites != null
                ? sprites.Resolve(slice.IsBomb ? "ui_card_icon_death" : slice.Reward.IconKey)
                : null;
            iconValue.preserveAspect = true;             // brief: do not stretch
            amountValue.text = slice.IsBomb ? string.Empty : $"x{slice.Reward.Amount}";
            // no amount on the bomb, so no empty pill floating on the cylinder either
            if (amountBg != null) amountBg.enabled = !slice.IsBomb;
        }

        /// <summary>
        /// Flash this chamber as the one that came up. Landing is the payoff of the whole
        /// spin, and without a mark on the winning chamber the wheel just stops and a popup
        /// appears — the player never sees the connection between the two.
        /// </summary>
        public void Pop()
        {
            if (iconValue == null) return;
            var rect = iconValue.rectTransform;
            ResetPop();
            rect.DOPunchScale(Vector3.one * 0.4f, 0.45f, 7, 0.8f)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void ResetPop()
        {
            if (iconValue == null) return;
            var rect = iconValue.rectTransform;
            rect.DOKill();
            rect.localScale = Vector3.one;
        }

        private void OnDisable() => ResetPop();

        /// <summary>
        /// Keep the chamber's contents upright, and pin the amount badge under its chamber.
        /// <para>
        /// The slice is rotated into place inside the rotor and the rotor spins, so anything
        /// that simply inherited both would ride the wheel upside down — which is how "x100"
        /// ended up mirrored at rest. Cancelling world rotation fixes the icon, but it is not
        /// enough for the badge: its POSITION is still carried around by the cylinder, which
        /// parks it inward of the chamber — below one chamber, to the left of the next, to
        /// the right of the one after. Placing it directly under the chamber in screen space
        /// gives all eight the same, readable arrangement.
        /// </para>
        /// </summary>
        private void LateUpdate()
        {
            if (iconValue == null) return;

            var icon = iconValue.rectTransform;
            icon.rotation = Quaternion.identity;

            // lossyScale off the slice, not the icon: the icon's own scale is punched by
            // Pop() and the badge must not jump when a landing plays
            Vector3 under = icon.position + Vector3.down * (BadgeDrop * transform.lossyScale.y);
            Straighten(amountBg != null ? amountBg.rectTransform : null, under);
            Straighten(amountValue != null ? amountValue.rectTransform : null, under);
        }

        private static void Straighten(RectTransform rect, Vector3 position)
        {
            if (rect == null) return;
            rect.rotation = Quaternion.identity;
            rect.position = position;
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref iconValue, "ui_image_slice_icon_value");
            Bind(ref amountValue, "ui_text_slice_amount_value");
            Bind(ref amountBg, "ui_image_slice_amount_bg");
        }
#endif
    }
}
