using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Application;

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

        /// <summary>
        /// Print the receipt as-is. The totals come from the banking itself, so this screen
        /// cannot promise loot the player does not actually keep.
        /// </summary>
        public void Show(BankReceipt receipt)
        {
            ShowRoot(root);
            if (summaryValue == null) return;

            summaryValue.text =
                $"YOU WALKED AWAY!\n\n+{receipt.Gold} Gold   +{receipt.Cash} Cash\n{receipt.Items} item(s) collected";
        }

        public void Hide() => HideRoot(root);

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
