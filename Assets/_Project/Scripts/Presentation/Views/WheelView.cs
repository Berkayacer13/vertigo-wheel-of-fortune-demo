using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wof.Data;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// The centerpiece: swaps the wheel art by tier, fills the slices, and spins the
    /// rotor (a child transform, never the root) to a pre-chosen index with DOTween.
    /// Buttons are subscribed in code — no editor OnClick.
    /// </summary>
    public sealed class WheelView : UiView
    {
        [SerializeField] private RectTransform rotor;   // ui_image_spin_rotor (animator lives here, NOT root)
        [SerializeField] private Image spinBronze;      // ui_image_spin_bronze
        [SerializeField] private Image spinSilver;      // ui_image_spin_silver
        [SerializeField] private Image spinGolden;      // ui_image_spin_golden
        [SerializeField] private Image indicator;       // ui_image_spin_indicator
        [SerializeField] private Button spinButton;     // ui_button_spin
        [SerializeField] private Button leaveButton;    // ui_button_leave
        [SerializeField] private TMP_Text titleValue;   // ui_text_wheel_title_value
        [SerializeField] private SliceView[] slices;    // ui_image_slice_*

        [SerializeField] private GameSettings settings;

        private Action _onSpin, _onLeave;

        public void BindInput(Action onSpin, Action onLeave)
        {
            _onSpin = onSpin;
            _onLeave = onLeave;
        }

        private void OnEnable()
        {
            if (spinButton != null) spinButton.onClick.AddListener(HandleSpin);
            if (leaveButton != null) leaveButton.onClick.AddListener(HandleLeave);
        }

        private void OnDisable()
        {
            if (spinButton != null) spinButton.onClick.RemoveListener(HandleSpin);
            if (leaveButton != null) leaveButton.onClick.RemoveListener(HandleLeave);
        }

        private void HandleSpin() => _onSpin?.Invoke();
        private void HandleLeave() => _onLeave?.Invoke();

        /// <summary>Swap art by tier and fill the slice content for a freshly built wheel.</summary>
        public void Render(WheelModel wheel)
        {
            if (spinBronze != null) spinBronze.enabled = wheel.Tier == WheelTier.Bronze;
            if (spinSilver != null) spinSilver.enabled = wheel.Tier == WheelTier.Silver;
            if (spinGolden != null) spinGolden.enabled = wheel.Tier == WheelTier.Golden;

            for (int i = 0; i < slices.Length; i++)
                slices[i].Render(i < wheel.SliceCount ? wheel.SliceAt(i) : null);
        }

        public void SetTitle(string title) { if (titleValue != null) titleValue.text = title; }
        public void SetLeaveEnabled(bool canLeave) { if (leaveButton != null) leaveButton.interactable = canLeave; }
        public void SetSpinEnabled(bool canSpin) { if (spinButton != null) spinButton.interactable = canSpin; }

        /// <summary>
        /// Spin the rotor to land slice <paramref name="index"/> under the top indicator.
        /// Returns a Task the state machine awaits. If it visually lands on the wrong
        /// slice for your child layout, negate <c>land</c>.
        /// </summary>
        public Task SpinTo(int index, int sliceCount)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (spinButton != null) spinButton.interactable = false;

            float sliceAngle = 360f / sliceCount;
            float land = sliceAngle * index;
            float jitter = UnityEngine.Random.Range(-sliceAngle * 0.25f, sliceAngle * 0.25f); // off dead-center
            float targetZ = settings.fullRotations * 360f + land + jitter;

            rotor.localRotation = Quaternion.identity;
            rotor.DOLocalRotate(new Vector3(0f, 0f, targetZ), settings.spinDuration, RotateMode.FastBeyond360)
                 .SetEase(settings.spinEase)
                 .OnComplete(() => tcs.TrySetResult(true));

            return tcs.Task;
        }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref rotor, "ui_image_spin_rotor");
            Bind(ref spinBronze, "ui_image_spin_bronze");
            Bind(ref spinSilver, "ui_image_spin_silver");
            Bind(ref spinGolden, "ui_image_spin_golden");
            Bind(ref indicator, "ui_image_spin_indicator");
            Bind(ref spinButton, "ui_button_spin");
            Bind(ref leaveButton, "ui_button_leave");
            Bind(ref titleValue, "ui_text_wheel_title_value");
            if (slices == null || slices.Length == 0)
                slices = GetComponentsInChildren<SliceView>(true);
        }
#endif
    }
}
