using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>Top-bar HUD: currencies, current zone, run reward count + inventory button.</summary>
    public sealed class HudView : UiView
    {
        [SerializeField] private TMP_Text goldValue;     // ui_text_currency_gold_value
        [SerializeField] private TMP_Text cashValue;     // ui_text_currency_cash_value
        [SerializeField] private ZoneTrackView zoneTrack; // ui_zone_track
        [SerializeField] private TMP_Text runCountValue; // ui_text_runcount_value
        [SerializeField] private Button inventoryButton; // ui_button_inventory

        private Action _onInventory;

        public void BindInventory(Action onInventory) => _onInventory = onInventory;

        private void OnEnable() { if (inventoryButton != null) inventoryButton.onClick.AddListener(HandleInventory); }
        private void OnDisable() { if (inventoryButton != null) inventoryButton.onClick.RemoveListener(HandleInventory); }
        private void HandleInventory() => _onInventory?.Invoke();

        public void SetCurrency(uint gold, uint cash)
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
            if (zoneTrack != null) zoneTrack.SetZone(zone);
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref goldValue, "ui_text_currency_gold_value");
            Bind(ref cashValue, "ui_text_currency_cash_value");
            Bind(ref zoneTrack, "ui_zone_track");
            Bind(ref runCountValue, "ui_text_runcount_value");
            Bind(ref inventoryButton, "ui_button_inventory");
        }
#endif
    }
}
