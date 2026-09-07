using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// Run-stash viewer ("envanter"): shows every reward collected in the current run
    /// as an icon grid — i.e. what the player is currently risking. Items are pushed in
    /// by the GameController from RewardWon events and cleared when the wallet empties
    /// (cash-out or bomb give-up), so the view never reaches into the logic layer.
    /// </summary>
    public sealed class InventoryView : UiView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform grid;        // ui_inventory_grid (GridLayoutGroup)
        [SerializeField] private RectTransform itemTemplate; // ui_item_template (inactive blueprint)
        [SerializeField] private TMP_Text emptyValue;       // ui_text_inventory_empty_value
        [SerializeField] private Button closeButton;        // ui_button_inventory_close
        [SerializeField] private SpriteRegistry sprites;

        private sealed class ItemStack
        {
            public GameObject Cell;
            public TMP_Text AmountText;
            public uint Total;
        }

        // one cell per reward id; repeat wins stack into the same cell (x10 + x25 -> x35)
        private readonly Dictionary<string, ItemStack> _stacks = new Dictionary<string, ItemStack>();
        private Action _onClose;

        public void BindClose(Action onClose) => _onClose = onClose;

        private void OnEnable() { if (closeButton != null) closeButton.onClick.AddListener(HandleClose); }
        private void OnDisable() { if (closeButton != null) closeButton.onClick.RemoveListener(HandleClose); }
        private void HandleClose() => _onClose?.Invoke();

        /// <summary>Add one collected reward, stacking onto an existing cell of the same id.</summary>
        public void AddItem(Reward reward)
        {
            if (itemTemplate == null) return;

            if (_stacks.TryGetValue(reward.Id, out var stack))
            {
                stack.Total += reward.Amount;
                if (stack.AmountText != null) stack.AmountText.text = $"x{stack.Total}";
                return;
            }

            var cell = UnityEngine.Object.Instantiate(itemTemplate.gameObject, grid);
            cell.name = $"ui_item_{reward.Id}";

            var icon = cell.transform.FindDeep("ui_image_item_icon_value")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = sprites != null ? sprites.Resolve(reward.IconKey) : null;
                icon.preserveAspect = true;
            }
            var amount = cell.transform.FindDeep("ui_text_item_amount_value")?.GetComponent<TMP_Text>();
            if (amount != null) amount.text = $"x{reward.Amount}";

            cell.SetActive(true);
            _stacks[reward.Id] = new ItemStack { Cell = cell, AmountText = amount, Total = reward.Amount };
            RefreshEmptyLabel();
        }

        /// <summary>Wallet went empty (cash-out or bomb give-up) — drop everything.</summary>
        public void Clear()
        {
            foreach (var stack in _stacks.Values) Destroy(stack.Cell);
            _stacks.Clear();
            RefreshEmptyLabel();
        }

        /// <summary>Distinct reward stacks currently shown.</summary>
        public int ItemCount => _stacks.Count;

        public void Show()
        {
            if (root != null) root.SetActive(true);
            RefreshEmptyLabel();
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private void RefreshEmptyLabel()
        {
            if (emptyValue != null) emptyValue.gameObject.SetActive(_stacks.Count == 0);
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref grid, "ui_inventory_grid");
            Bind(ref itemTemplate, "ui_item_template");
            Bind(ref emptyValue, "ui_text_inventory_empty_value");
            Bind(ref closeButton, "ui_button_inventory_close");
            if (root == null) root = gameObject;
        }
#endif
    }
}
