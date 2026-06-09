using TMPro;
using UnityEngine;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>Top-bar HUD: currencies, current zone and the run reward count.</summary>
    public sealed class HudView : UiView
    {
        [SerializeField] private TMP_Text goldValue;     // ui_text_currency_gold_value
        [SerializeField] private TMP_Text cashValue;     // ui_text_currency_cash_value
        [SerializeField] private TMP_Text zoneValue;     // ui_text_zone_value
        [SerializeField] private TMP_Text runCountValue; // ui_text_runcount_value

        public void SetCurrency(int gold, int cash)
        {
            if (goldValue != null) goldValue.text = gold.ToString();
            if (cashValue != null) cashValue.text = cash.ToString();
        }

        public void SetRunCount(int count)
        {
            if (runCountValue != null) runCountValue.text = count.ToString();
        }

        public void SetZone(int zone, ZoneType type)
        {
            if (zoneValue != null) zoneValue.text = $"ZONE {zone}";
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref goldValue, "ui_text_currency_gold_value");
            Bind(ref cashValue, "ui_text_currency_cash_value");
            Bind(ref zoneValue, "ui_text_zone_value");
            Bind(ref runCountValue, "ui_text_runcount_value");
        }
#endif
    }
}
