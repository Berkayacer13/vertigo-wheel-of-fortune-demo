using System;
using DG.Tweening;
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

        private const float CountUpTime = 0.45f;

        /// <summary>Neutral grey while the run is worth nothing; amber once there is a stake.</summary>
        private static readonly Color NoStake = new Color(0.70f, 0.72f, 0.76f);
        private static readonly Color AtStake = new Color(1f, 0.66f, 0.20f);

        private Action _onInventory;
        private Tweener _goldRoll, _cashRoll;
        private uint _gold, _cash;
        private int _runCount = -1;
        private bool _primed;

        public void BindInventory(Action onInventory) => _onInventory = onInventory;

        private void OnEnable() { if (inventoryButton != null) inventoryButton.onClick.AddListener(HandleInventory); }
        private void OnDisable() { if (inventoryButton != null) inventoryButton.onClick.RemoveListener(HandleInventory); }
        private void HandleInventory() => _onInventory?.Invoke();

        private void OnDestroy()
        {
            if (_goldRoll != null && _goldRoll.IsActive()) _goldRoll.Kill();
            if (_cashRoll != null && _cashRoll.IsActive()) _cashRoll.Kill();
        }

        public void SetCurrency(uint gold, uint cash)
        {
            // the opening balance is a fact, not an event — only count up once the player
            // has actually earned or spent something
            if (!_primed)
            {
                _primed = true;
                _gold = gold; _cash = cash;
                Write(goldValue, gold);
                Write(cashValue, cash);
                return;
            }

            RollTo(ref _goldRoll, goldValue, _gold, gold);
            RollTo(ref _cashRoll, cashValue, _cash, cash);
            _gold = gold;
            _cash = cash;
        }

        /// <summary>
        /// Count a balance up (or down) instead of snapping. A number that jumps from 100 to
        /// 350 is easy to miss; one that rolls tells the player they were paid, and the pop
        /// aims the eye at the readout that changed.
        /// </summary>
        private static void RollTo(ref Tweener roll, TMP_Text label, uint from, uint to)
        {
            if (label == null || from == to) return;

            if (roll != null && roll.IsActive()) roll.Kill();
            // DOVirtual.Float rather than a typed tween: DOTween has no uint plugin, and the
            // readout only ever needs a rounded number to print.
            roll = DOVirtual.Float(from, to, CountUpTime, v => Write(label, (uint)Mathf.Round(v)))
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(label.gameObject)
                .OnComplete(() => Write(label, to));   // land on the exact figure, never 349

            Pop(label.rectTransform, to > from ? 0.24f : 0.12f);
        }

        private static void Write(TMP_Text label, uint value)
        {
            if (label != null) label.text = value.ToString();
        }

        /// <summary>How many rewards the next bomb would take — the run's stake.</summary>
        public void SetRunCount(int count)
        {
            if (runCountValue == null || count == _runCount) return;

            bool grew = count > _runCount && _runCount >= 0;
            _runCount = count;
            runCountValue.text = $"AT RISK: {count}";
            // colour carries the warning: a run with nothing on it should not look alarming,
            // and a run with eight rewards on it should
            runCountValue.color = count > 0 ? AtStake : NoStake;
            if (grew) Pop(runCountValue.rectTransform, 0.22f);
        }

        private static void Pop(RectTransform rect, float strength)
        {
            if (rect == null) return;
            rect.DOKill(true);
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * strength, 0.34f, 8, 0.9f)
                .SetUpdate(true)
                .SetLink(rect.gameObject);
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
