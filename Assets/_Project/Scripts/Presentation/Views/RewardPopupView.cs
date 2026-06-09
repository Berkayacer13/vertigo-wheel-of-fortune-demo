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
        [SerializeField] private SpriteRegistry sprites;

        private Action _onCollect;

        public void BindCollect(Action onCollect) => _onCollect = onCollect;

        private void OnEnable() { if (collectButton != null) collectButton.onClick.AddListener(HandleCollect); }
        private void OnDisable() { if (collectButton != null) collectButton.onClick.RemoveListener(HandleCollect); }
        private void HandleCollect() => _onCollect?.Invoke();

        public void Show(Reward reward)
        {
            if (root != null) root.SetActive(true);
            if (iconValue != null)
            {
                iconValue.sprite = sprites != null ? sprites.Resolve(reward.IconKey) : null;
                iconValue.preserveAspect = true;
            }
            if (amountValue != null) amountValue.text = $"x{reward.Amount}";
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
            if (root == null) root = gameObject;
        }
#endif
    }
}
