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
        [SerializeField] private SpriteRegistry sprites; // assigned on the prefab/asset

        public void Render(WheelSlice slice)
        {
            if (slice == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            iconValue.sprite = sprites != null
                ? sprites.Resolve(slice.IsBomb ? "ui_card_icon_death" : slice.Reward.IconKey)
                : null;
            iconValue.preserveAspect = true;             // brief: do not stretch
            amountValue.text = slice.IsBomb ? string.Empty : $"x{slice.Reward.Amount}";
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref iconValue, "ui_image_slice_icon_value");
            Bind(ref amountValue, "ui_text_slice_amount_value");
        }
#endif
    }
}
