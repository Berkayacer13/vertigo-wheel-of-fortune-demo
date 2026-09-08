using System;
using DG.Tweening;
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

        private Action _onReviveGold, _onReviveAd, _onReviveShield, _onGiveUp;
        private TMP_Text _recoveryLabel;
        private RectTransform _bombIcon;
        private bool _hasShield;

        public void BindInput(Action onReviveGold, Action onReviveAd, Action onReviveShield, Action onGiveUp)
        {
            _onReviveGold = onReviveGold;
            _onReviveAd = onReviveAd;
            _onReviveShield = onReviveShield;
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
        private void HandleReviveAd()
        {
            if (_hasShield) _onReviveShield?.Invoke();
            else _onReviveAd?.Invoke();
        }
        private void HandleGiveUp() => _onGiveUp?.Invoke();

        public void Show(uint reviveCost, bool hasShield)
        {
            _hasShield = hasShield;
            if (root != null) root.SetActive(true);
            if (reviveCostValue != null) reviveCostValue.text = reviveCost.ToString();

            if (_bombIcon == null)
                _bombIcon = transform.FindDeep("ui_image_bomb_icon") as RectTransform;
            if (_bombIcon != null)
            {
                _bombIcon.DOKill();
                _bombIcon.localScale = Vector3.one;
                _bombIcon.DOPunchScale(Vector3.one * 0.2f, 0.45f, 8, 0.75f);
                var image = _bombIcon.GetComponent<Image>();
                if (image != null)
                {
                    image.DOKill();
                    image.color = Color.white;
                    image.DOColor(new Color(1f, 0.18f, 0.12f), 0.08f)
                        .SetLoops(4, LoopType.Yoyo);
                }
            }

            if (_recoveryLabel == null && reviveAdButton != null)
                _recoveryLabel = reviveAdButton.GetComponentInChildren<TMP_Text>(true);
            if (_recoveryLabel != null) _recoveryLabel.text = hasShield ? "USE SHIELD" : "REVIVE (AD)";

            if (hasShield && reviveAdButton != null)
            {
                reviveAdButton.transform.DOKill();
                reviveAdButton.transform.localScale = Vector3.one;
                reviveAdButton.transform.DOPunchScale(Vector3.one * 0.12f, 0.55f, 7, 0.7f);
            }
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
