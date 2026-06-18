using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>Win popup: shows the reward and waits for Collect. VFX live on a child transform.</summary>
    public sealed class RewardPopupView : UiView
    {
        [SerializeField] private GameObject root;        // panel toggled on/off
        [SerializeField] private RectTransform card;     // animated child, NOT the root
        [SerializeField] private Image iconValue;        // ui_image_reward_icon_value
        [SerializeField] private TMP_Text amountValue;   // ui_text_reward_amount_value
        [SerializeField] private Button collectButton;   // ui_button_collect
        [SerializeField] private Button leaveButton;      // ui_button_reward_leave (safe/super only)
        [SerializeField] private SpriteRegistry sprites;

        private Action _onCollect, _onLeave;
        private TMP_Text _collectLabel;

        public void BindCollect(Action onCollect) => _onCollect = onCollect;
        public void BindLeave(Action onLeave) => _onLeave = onLeave;

        private void OnEnable()
        {
            if (collectButton != null) collectButton.onClick.AddListener(HandleCollect);
            if (leaveButton != null) leaveButton.onClick.AddListener(HandleLeave);
        }

        private void OnDisable()
        {
            if (collectButton != null) collectButton.onClick.RemoveListener(HandleCollect);
            if (leaveButton != null) leaveButton.onClick.RemoveListener(HandleLeave);
        }

        private void HandleCollect() => _onCollect?.Invoke();
        private void HandleLeave() => _onLeave?.Invoke();

        /// <param name="canLeave">true on safe/super zones — reveals the cash-out button and
        /// turns Collect into "COLLECT &amp; CONTINUE" so the choice reads clearly.</param>
        public void Show(Reward reward, bool canLeave)
        {
            if (root != null) root.SetActive(true);
            if (iconValue != null)
            {
                iconValue.sprite = sprites != null ? sprites.Resolve(reward.IconKey) : null;
                iconValue.preserveAspect = true;
            }
            if (amountValue != null) amountValue.text = $"x{reward.Amount}";

            if (leaveButton != null) leaveButton.gameObject.SetActive(canLeave);
            if (_collectLabel == null && collectButton != null)
            {
                _collectLabel = collectButton.GetComponentInChildren<TMP_Text>(true);
                if (_collectLabel != null)
                {
                    _collectLabel.enableAutoSizing = true; // "COLLECT & CONTINUE" is long; shrink to fit
                    _collectLabel.fontSizeMin = 24;
                    _collectLabel.fontSizeMax = 40;
                }
            }
            if (_collectLabel != null) _collectLabel.text = canLeave ? "COLLECT & CONTINUE" : "COLLECT";

            // centre Collect when alone; share the row with Leave on safe/super zones
            if (collectButton != null)
            {
                var rt = (RectTransform)collectButton.transform;
                rt.anchoredPosition = new Vector2(canLeave ? -205f : 0f, rt.anchoredPosition.y);
            }

            if (card != null)
            {
                card.localScale = Vector3.one;
                card.DOPunchScale(Vector3.one * 0.2f, 0.4f, 6, 0.8f);
            }
        }

        public void Hide() { if (root != null) root.SetActive(false); }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref card, "ui_image_reward_card");
            Bind(ref iconValue, "ui_image_reward_icon_value");
            Bind(ref amountValue, "ui_text_reward_amount_value");
            Bind(ref collectButton, "ui_button_collect");
            Bind(ref leaveButton, "ui_button_reward_leave");
            if (root == null) root = gameObject;
        }
#endif
    }
}
