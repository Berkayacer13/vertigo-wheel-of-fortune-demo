using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>
    /// Loss/revive screen: "a bomb exploded…". Three actions map to the brief screenshot:
    /// GIVE UP / REVIVE (gold) / REVIVE (ad) — the last one relabelled USE SHIELD while the
    /// player holds one. Which of the two it spends is the state's decision, not this view's.
    /// </summary>
    public sealed class BombExplodedView : UiView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text reviveCostValue;  // ui_text_revive_cost_value
        [SerializeField] private Button giveUpButton;       // ui_button_giveup
        [SerializeField] private Button reviveGoldButton;   // ui_button_revive_gold
        [SerializeField] private Button reviveAdButton;     // ui_button_revive_ad

        /// <summary>How long the wheel stays uncovered after the bomb lands (seconds).</summary>
        private const float LossBeat = 0.5f;

        /// <summary>Cyan wash that marks the revive button as a shield spend, not an ad.</summary>
        private static readonly Color ShieldTint = new Color(0.45f, 0.85f, 1f);

        private Action _onReviveGold, _onReviveFree, _onGiveUp;
        private TMP_Text _recoveryLabel;
        private RectTransform _bombIcon;

        /// <summary>The revive button's visible body. Its root belongs to layout, not animation.</summary>
        private Transform ReviveAdBody =>
            reviveAdButton != null && reviveAdButton.targetGraphic != null
                ? reviveAdButton.targetGraphic.transform
                : null;

        public void BindInput(Action onReviveGold, Action onReviveFree, Action onGiveUp)
        {
            _onReviveGold = onReviveGold;
            _onReviveFree = onReviveFree;
            _onGiveUp = onGiveUp;
        }

        private void OnEnable()
        {
            if (reviveGoldButton != null) reviveGoldButton.onClick.AddListener(HandleReviveGold);
            if (reviveAdButton != null) reviveAdButton.onClick.AddListener(HandleReviveFree);
            if (giveUpButton != null) giveUpButton.onClick.AddListener(HandleGiveUp);
        }

        private void OnDisable()
        {
            if (reviveGoldButton != null) reviveGoldButton.onClick.RemoveListener(HandleReviveGold);
            if (reviveAdButton != null) reviveAdButton.onClick.RemoveListener(HandleReviveFree);
            if (giveUpButton != null) giveUpButton.onClick.RemoveListener(HandleGiveUp);
        }

        private void HandleReviveGold() => _onReviveGold?.Invoke();
        private void HandleReviveFree() => _onReviveFree?.Invoke();
        private void HandleGiveUp() => _onGiveUp?.Invoke();

        /// <param name="shieldCount">Shields in the run wallet; each one survives one bomb.</param>
        public void Show(uint reviveCost, int shieldCount)
        {
            bool hasShield = shieldCount > 0;
            // longer hold than the reward popup: the screen shake and the bomb chamber
            // under the indicator are the whole point of losing a run, and a screen that
            // slams up instantly hides both.
            ShowRoot(root, null, LossBeat);
            if (reviveCostValue != null) reviveCostValue.text = reviveCost.ToString();

            if (_bombIcon == null)
                _bombIcon = transform.FindDeep("ui_image_bomb_icon") as RectTransform;
            if (_bombIcon != null)
            {
                _bombIcon.DOKill();
                _bombIcon.localScale = Vector3.one;
                // every flourish waits out LossBeat too, or it plays while the screen is
                // still transparent and the player never sees it
                _bombIcon.DOPunchScale(Vector3.one * 0.2f, 0.45f, 8, 0.75f).SetDelay(LossBeat);
                var image = _bombIcon.GetComponent<Image>();
                if (image != null)
                {
                    image.DOKill();
                    image.color = Color.white;
                    image.DOColor(new Color(1f, 0.18f, 0.12f), 0.08f)
                        .SetLoops(4, LoopType.Yoyo)
                        .SetDelay(LossBeat);
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
                // tint the body the Button draws with; the root carries no graphic
                if (reviveAdButton.targetGraphic != null)
                    reviveAdButton.targetGraphic.color = hasShield ? ShieldTint : Color.white;

                var body = ReviveAdBody;
                if (hasShield && body != null)
                {
                    // punch the body, never the button's root (brief: no UI animation on roots)
                    body.DOKill();
                    body.localScale = Vector3.one;
                    body.DOPunchScale(Vector3.one * 0.12f, 0.55f, 7, 0.7f)
                        .SetDelay(LossBeat);
                }
            }
        }

        public void Hide()
        {
            KillFeedbackTweens();
            HideRoot(root);
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

            var body = ReviveAdBody;
            if (body != null)
            {
                body.DOKill();
                body.localScale = Vector3.one;
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
