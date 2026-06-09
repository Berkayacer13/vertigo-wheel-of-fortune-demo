using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>
    /// Loss/revive screen: "a bomb exploded…". Three actions map to the brief screenshot:
    /// GIVE UP / REVIVE (gold) / REVIVE (ad).
    /// </summary>
    public sealed class BombExplodedView : UiView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text reviveCostValue;  // ui_text_revive_cost_value
        [SerializeField] private Button giveUpButton;       // ui_button_giveup
        [SerializeField] private Button reviveGoldButton;   // ui_button_revive_gold
        [SerializeField] private Button reviveAdButton;     // ui_button_revive_ad

        private Action _onReviveGold, _onReviveAd, _onGiveUp;

        public void BindInput(Action onReviveGold, Action onReviveAd, Action onGiveUp)
        {
            _onReviveGold = onReviveGold;
            _onReviveAd = onReviveAd;
            _onGiveUp = onGiveUp;
        }

        private void OnEnable()
        {
            if (reviveGoldButton != null) reviveGoldButton.onClick.AddListener(HandleReviveGold);
            if (reviveAdButton != null) reviveAdButton.onClick.AddListener(HandleReviveAd);
            if (giveUpButton != null) giveUpButton.onClick.AddListener(HandleGiveUp);
        }

        private void OnDisable()
        {
            if (reviveGoldButton != null) reviveGoldButton.onClick.RemoveListener(HandleReviveGold);
            if (reviveAdButton != null) reviveAdButton.onClick.RemoveListener(HandleReviveAd);
            if (giveUpButton != null) giveUpButton.onClick.RemoveListener(HandleGiveUp);
        }

        private void HandleReviveGold() => _onReviveGold?.Invoke();
        private void HandleReviveAd() => _onReviveAd?.Invoke();
        private void HandleGiveUp() => _onGiveUp?.Invoke();

        public void Show(int reviveCost)
        {
            if (root != null) root.SetActive(true);
            if (reviveCostValue != null) reviveCostValue.text = reviveCost.ToString();
        }

        public void Hide() { if (root != null) root.SetActive(false); }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref reviveCostValue, "ui_text_revive_cost_value");
            Bind(ref giveUpButton, "ui_button_giveup");
            Bind(ref reviveGoldButton, "ui_button_revive_gold");
            Bind(ref reviveAdButton, "ui_button_revive_ad");
            if (root == null) root = gameObject;
        }
#endif
    }
}
