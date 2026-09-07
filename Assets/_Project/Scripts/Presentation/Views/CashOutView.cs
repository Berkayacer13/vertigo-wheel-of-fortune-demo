using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>Shown when the player walks away on a safe/super zone: the collected loot + confirm.</summary>
    public sealed class CashOutView : UiView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text summaryValue;   // ui_text_cashout_summary_value
        [SerializeField] private Button confirmButton;    // ui_button_cashout_confirm

        private Action _onConfirm;

        public void BindConfirm(Action onConfirm) => _onConfirm = onConfirm;

        private void OnEnable() { if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm); }
        private void OnDisable() { if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm); }
        private void HandleConfirm() => _onConfirm?.Invoke();

        public void Show(IReadOnlyList<Reward> banked)
        {
            if (root != null) root.SetActive(true);
            if (summaryValue == null) return;

            // currencies go straight to the balances; everything else is "items"
            uint gold = 0, cash = 0;
            int items = 0;
            foreach (var r in banked)
            {
                if (r.Kind == RewardKind.Gold) gold += r.Amount;
                else if (r.Kind == RewardKind.Cash) cash += r.Amount;
                else items++;
            }
            summaryValue.text =
                $"YOU WALKED AWAY!\n\n+{gold} Gold   +{cash} Cash\n{items} item(s) collected";
        }

        public void Hide() { if (root != null) root.SetActive(false); }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref summaryValue, "ui_text_cashout_summary_value");
            Bind(ref confirmButton, "ui_button_cashout_confirm");
            if (root == null) root = gameObject;
        }
#endif
    }
}
