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
        [SerializeField] private TMP_Text subtitleValue; // ui_text_wheel_subtitle_value
        [SerializeField] private SliceView[] slices;    // ui_image_slice_*

        [SerializeField] private GameSettings settings;

        private Action _onSpin, _onLeave;

        // Cylinder art is 900 across, so its rim sits 450 from the rotor centre before any
        // scaling. Every indicator/title offset below is that radius plus clearance.
        private const float RotorArt = 900f;

        // ---- layout budget (canvas units, portrait) --------------------------
        private const float TopReserved = 320f;   // HUD row + zone track live up here
        private const float BottomPad = 110f;
        private const float TitleBand = 190f;     // title + indicator clearance above the rim
        private const float SubtitleBand = 120f;  // risk line clearance below the rim

        // ---- spin feel -------------------------------------------------------
        private const float WindBack = 14f;       // degrees of anticipation before the launch
        private const float WindTime = 0.16f;
        private const float KickDecay = 7f;       // indicator kick per second
        private const float KickAngle = -13f;
        private const float KickScale = 0.14f;

        private static readonly Color TitleSafe = new Color(0.72f, 0.86f, 0.95f);
        private static readonly Color TitleSuper = new Color(1f, 0.86f, 0.35f);

        private Vector2 _laidOutSize;
        private bool _hasLaidOut;

        private Sequence _spin;
        private bool _spinning;
        private float _tickAngle;
        private int _lastTick;
        private float _kick;
        private TMP_Text _leaveLabel;

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

        /// <summary>
        /// Re-arrange for the space actually available, then drive the per-frame spin feel.
        /// The layout is measured off the parent rect rather than Screen.width/height so it
        /// also reacts to a Game-view resize and to whatever the safe area leaves behind —
        /// a portrait-to-portrait resolution change moves the budget just as much as a
        /// rotation does.
        /// </summary>
        private void Update()
        {
            Relayout();
            TickIndicator();
        }

        private void Relayout()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;

            Vector2 size = parent.rect.size;
            // the rect is still zero on the first frame — bail rather than cache a layout
            // built from nothing
            if (size.x <= 0f || size.y <= 0f) return;
            if (_hasLaidOut && (size - _laidOutSize).sqrMagnitude < 1f) return;

            if (size.x > size.y) ApplyLandscape(size);
            else ApplyPortrait(size);

            _laidOutSize = size;
            _hasLaidOut = true;
        }

        /// <summary>
        /// The stacked phone layout: controls pinned above the bottom edge, cylinder centred
        /// in whatever is left between them and the HUD.
        /// <para>
        /// Nothing here can be a constant. The canvas reference is 1080x1920 on Expand, so a
        /// portrait canvas is 1920 tall on 4:3 but 2400 on 20:9 — the old fixed offsets sized
        /// the whole screen for 4:3 and left a tall phone with ~500 units of dead space above
        /// the wheel and another ~500 below the buttons.
        /// </para>
        /// </summary>
        private void ApplyPortrait(Vector2 size)
        {
            float width = size.x;
            float height = size.y;

            var root = (RectTransform)transform;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = size;

            // 1) pin the controls to the bottom of the screen
            float leaveY = -height * 0.5f + BottomPad + 55f;
            float spinY = leaveY + 143f;
            Move(spinButton, new Vector2(0, spinY), new Vector2(Mathf.Min(width * 0.62f, 560f), 140), 48);
            Move(leaveButton, new Vector2(0, leaveY), new Vector2(Mathf.Min(width * 0.62f, 560f), 110), 32);

            // 2) the cylinder gets everything between the HUD and the buttons
            float areaTop = height * 0.5f - TopReserved;
            float areaBottom = spinY + 140f;
            float avail = areaTop - areaBottom;

            // The cylinder is not the only thing in this band: the zone title sits above the
            // rim and the risk line below it, so both bands come out of the budget before the
            // wheel is sized. Sizing on the rim alone put the risk line on top of SPIN at 4:3.
            float scale = Mathf.Clamp(
                Mathf.Min((avail - TitleBand - SubtitleBand) / RotorArt, width * 0.94f / RotorArt),
                0.62f, 1.18f);
            float radius = RotorArt * 0.5f * scale;
            float block = RotorArt * scale + TitleBand + SubtitleBand;
            float wheelY = areaBottom + SubtitleBand + (avail - block) * 0.5f + radius;

            PlaceWheel(0f, wheelY, scale, radius, titleFont: 64, subtitleFont: 32);
        }

        /// <summary>
        /// The wide layout: cylinder in the left half, controls in the right half. Same
        /// reasoning as portrait — the logical canvas runs from ~2560 (4:3) to ~4160 (21:9)
        /// wide, so horizontal numbers are fractions of the width actually available.
        /// </summary>
        private void ApplyLandscape(Vector2 size)
        {
            float width = size.x;
            float height = size.y;

            var root = (RectTransform)transform;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = size;

            // fill 72% of the height with the cylinder, then centre it in the left half.
            // Height is the binding constraint in landscape — the left half is always wider
            // than it is tall — so this is as large as the title and risk line allow.
            float scale = height * 0.72f / RotorArt;
            float radius = RotorArt * 0.5f * scale;

            PlaceWheel(-width * 0.24f, -height * 0.04f, scale, radius, titleFont: 76, subtitleFont: 40);

            float buttonX = width * 0.24f;
            Move(spinButton, new Vector2(buttonX, 90), new Vector2(640, 180), 62);
            Move(leaveButton, new Vector2(buttonX, -120), new Vector2(640, 150), 42);
        }

        /// <summary>Position the rotor and everything that hangs off its rim.</summary>
        private void PlaceWheel(float x, float y, float scale, float radius, float titleFont, float subtitleFont)
        {
            if (rotor != null)
            {
                rotor.anchoredPosition = new Vector2(x, y);
                rotor.localScale = Vector3.one * scale;
            }

            // the indicator sits ON the rim, like the hammer of a revolver
            Move(indicator, new Vector2(x, y + radius), new Vector2(80, 110) * scale);

            if (titleValue != null)
            {
                titleValue.rectTransform.anchoredPosition = new Vector2(x, y + radius + 120f);
                titleValue.fontSize = titleFont;
            }
            if (subtitleValue != null)
            {
                // under the wheel, in the gap above the SPIN button
                subtitleValue.rectTransform.anchoredPosition = new Vector2(x, y - radius - 58f);
                subtitleValue.fontSize = subtitleFont;
            }
        }

        private static void Move(Graphic target, Vector2 position, Vector2 size)
        {
            if (target == null) return;
            target.rectTransform.anchoredPosition = position;
            target.rectTransform.sizeDelta = size;
        }

        /// <summary>Moves a button and resizes its label to match, so text never outgrows it.</summary>
        private static void Move(Button button, Vector2 position, Vector2 size, float fontSize)
        {
            if (button == null) return;
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.fontSize = fontSize;
        }

        /// <summary>Swap art by tier and fill the slice content for a freshly built wheel.</summary>
        public void Render(WheelModel wheel)
        {
            if (spinBronze != null) spinBronze.enabled = wheel.Tier == WheelTier.Bronze;
            if (spinSilver != null) spinSilver.enabled = wheel.Tier == WheelTier.Silver;
            if (spinGolden != null) spinGolden.enabled = wheel.Tier == WheelTier.Golden;

            for (int i = 0; i < slices.Length; i++)
                slices[i].Render(i < wheel.SliceCount ? wheel.SliceAt(i) : null);
        }

        /// <summary>
        /// Headline the zone the player is standing in. The safe/super zones are the only
        /// bomb-free spins in the game (R3) and the wheel art alone was carrying that news;
        /// the subtitle says it outright, because a player who does not know this spin is
        /// free cannot make the risk decision the game is built around.
        /// </summary>
        public void SetZone(ZoneType type)
        {
            if (titleValue != null)
            {
                titleValue.text = type switch
                {
                    ZoneType.Super => "GOLDEN SPIN",
                    ZoneType.Safe => "SILVER SPIN",
                    _ => "SPIN",
                };
                titleValue.color = type switch
                {
                    ZoneType.Super => TitleSuper,
                    ZoneType.Safe => TitleSafe,
                    _ => UiPalette.Gold,
                };
            }

            if (subtitleValue == null) return;
            bool safe = type != ZoneType.Normal;
            subtitleValue.text = safe ? "NO BOMB — THIS SPIN IS FREE" : "A BOMB WIPES YOUR RUN";
            subtitleValue.color = safe
                ? UiPalette.SafeGreen
                : new Color(0.85f, 0.45f, 0.42f);
        }

        /// <summary>
        /// Say what the LEAVE button is waiting for. Greying it out told the player it was
        /// unavailable but never that walking away unlocks on every 5th zone, so the one
        /// decision the game asks of them looked like a broken button.
        /// </summary>
        public void SetLeavePrompt(int zonesUntilLeave)
        {
            if (LeaveLabel() == null) return;
            _leaveLabel.text = zonesUntilLeave <= 0 ? "LEAVE & COLLECT"
                : zonesUntilLeave == 1 ? "SAFE ZONE IS NEXT"
                : $"SAFE ZONE IN {zonesUntilLeave}";
        }

        private TMP_Text LeaveLabel()
        {
            if (_leaveLabel == null && leaveButton != null)
                _leaveLabel = leaveButton.GetComponentInChildren<TMP_Text>(true);
            return _leaveLabel;
        }

        public void SetLeaveEnabled(bool canLeave) { if (leaveButton != null) leaveButton.interactable = canLeave; }
        public void SetSpinEnabled(bool canSpin) { if (spinButton != null) spinButton.interactable = canSpin; }

        /// <summary>Mark the chamber the wheel stopped on, before any screen covers it.</summary>
        public void HighlightSlice(int index)
        {
            _kick = 1f;   // one last knock as the chamber seats under the indicator
            if (slices != null && index >= 0 && index < slices.Length)
                slices[index].Pop();
        }

        /// <summary>
        /// Nudge the indicator every time a chamber passes under it. A revolver cylinder that
        /// glides to a halt in dead silence reads as a slideshow; the indicator being knocked
        /// aside by each chamber is what makes the wheel feel mechanical, and it doubles as
        /// the player's read on how fast the wheel is still travelling.
        /// </summary>
        private void TickIndicator()
        {
            if (_spinning && rotor != null && _tickAngle > 0f)
            {
                int tick = Mathf.FloorToInt(rotor.localEulerAngles.z / _tickAngle);
                if (tick != _lastTick)
                {
                    _lastTick = tick;
                    _kick = 1f;
                }
            }

            if (indicator == null || _kick <= 0f) return;

            _kick = Mathf.MoveTowards(_kick, 0f, Time.deltaTime * KickDecay);
            float k = _kick * _kick;   // quadratic, so the last few ticks read as separate knocks
            var rect = indicator.rectTransform;
            rect.localRotation = Quaternion.Euler(0f, 0f, KickAngle * k);
            rect.localScale = Vector3.one * (1f + KickScale * k);

            if (_kick > 0f) return;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

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

            if (_spin != null && _spin.IsActive()) _spin.Kill();
            rotor.DOKill();
            rotor.localRotation = Quaternion.identity;

            _tickAngle = sliceAngle;
            _lastTick = int.MinValue;
            _spinning = true;

            _spin = DOTween.Sequence()
                .SetLink(gameObject)
                // wind back against the throw first: the recoil is what sells the launch,
                // and it costs a sixth of a second before the spin the player asked for
                .Append(rotor.DOLocalRotate(new Vector3(0f, 0f, -WindBack), WindTime).SetEase(Ease.OutQuad))
                .Append(rotor.DOLocalRotate(new Vector3(0f, 0f, targetZ), settings.spinDuration,
                            RotateMode.FastBeyond360)
                        .SetEase(settings.spinEase))
                .OnComplete(() =>
                {
                    _spinning = false;
                    tcs.TrySetResult(true);
                })
                // a killed tween (scene unload, DOKill, disable) must still release the
                // awaiting state, or the FSM sits in Spinning forever with SPIN disabled
                .OnKill(() =>
                {
                    _spinning = false;
                    tcs.TrySetResult(false);
                });

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
            Bind(ref subtitleValue, "ui_text_wheel_subtitle_value");
            if (slices == null || slices.Length == 0)
                slices = GetComponentsInChildren<SliceView>(true);
        }
#endif
    }
}
