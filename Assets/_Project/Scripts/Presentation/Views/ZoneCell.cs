using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>
    /// One number on the top zone-track. Shows its zone number, colour-coded by type,
    /// and reveals a highlight box when it is the active zone.
    /// </summary>
    public sealed class ZoneCell : UiView
    {
        [SerializeField] private Image highlight;  // ui_image_zone_cell_highlight
        [SerializeField] private TMP_Text label;   // ui_text_zone_cell_value

        public void Set(int zone, Color color, bool current)
        {
            if (label != null)
            {
                label.text = zone.ToString();
                label.color = current ? Color.white : color;
                label.fontStyle = current ? FontStyles.Bold : FontStyles.Normal;
            }
            if (highlight != null) highlight.enabled = current;
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref highlight, "ui_image_zone_cell_highlight");
            Bind(ref label, "ui_text_zone_cell_value");
        }
#endif
    }
}
