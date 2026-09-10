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

        /// <summary>Cyan wash that marks the revive button as a shield spend, not an ad.</summary>
        private static readonly Color ShieldTint = new Color(0.45f, 0.85f, 1f);

        private Action _onReviveGold, _onReviveAd, _onGiveUp;
        private Func<bool> _onReviveShield;
        private TMP_Text _recoveryLabel;
        private RectTransform _bombIcon;
        private bool _hasShield;

        public void BindInput(Action onReviveGold, Action onReviveAd, Func<bool> onReviveShield, Action onGiveUp)
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
            // the shield is the better offer, so it takes this button when the player holds
            // one — but if the wallet has none left, fall through to the ad rather than
            // leaving a button that does nothing
            if (_hasShield && _onReviveShield != null && _onReviveShield()) return;
            _onReviveAd?.Invoke();
        }
        private void HandleGiveUp() => _onGiveUp?.Invoke();

        /// <param name="shieldCount">Shields in the run wallet; each one survives one bomb.</param>
        public void Show(uint reviveCost, int shieldCount)
        {
            bool hasShield = shieldCount > 0;
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
            if (_recoveryLabel != null)
                _recoveryLabel.text = hasShield ? $"USE SHIELD ({shieldCount})" : "REVIVE (AD)";

            // This button changes meaning when a shield is held, so change how it LOOKS too —
            // a relabel alone leaves the player tapping the same orange button they have used
            // for ad revives all run and silently spending a shield they earned.
            if (reviveAdButton != null)
            {
                var buttonImage = reviveAdButton.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = hasShield ? ShieldTint : Color.white;

                if (hasShield)
                {
                    reviveAdButton.transform.DOKill();
                    reviveAdButton.transform.localScale = Vector3.one;
                    reviveAdButton.transform.DOPunchScale(Vector3.one * 0.12f, 0.55f, 7, 0.7f);
                }
            }
        }

        public void Hide()
        {
            KillFeedbackTweens();
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy() => KillFeedbackTweens();

        /// <summary>
        /// Stop the bomb/shield feedback and restore the neutral pose. Without this the
        /// tweens keep writing to the icon after the screen is hidden, so the next bomb
        /// can open mid-flash on a half-scaled icon.
        /// </summary>
        private void KillFeedbackTweens()
        {
            if (_bombIcon != null)
            {
                _bombIcon.DOKill();
                _bombIcon.localScale = Vector3.one;
                var image = _bombIcon.GetComponent<Image>();
                if (image != null)
                {
                    image.DOKill();
                    image.color = Color.white;
                }
            }

            if (reviveAdButton != null)
            {
                reviveAdButton.transform.DOKill();
                reviveAdButton.transform.localScale = Vector3.one;
            }
        }

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
