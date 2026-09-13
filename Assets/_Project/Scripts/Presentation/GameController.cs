using System;
using System.Collections.Generic;
using UnityEngine;
using Wof.Application;
using Wof.Data;

namespace Wof.Presentation
{
    /// <summary>
    /// Composition root. Holds the scene references, builds the game context and the state
    /// machine, and hands each area of the screen to the presenter that owns it. It contains
    /// no game rules and no view logic: event -> view wiring lives in the presenters, and every
    /// player input goes through <see cref="StateInput"/>.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ZoneTuning tuning;
        [SerializeField] private GameSettings settings;

        [Header("Views")]
        [SerializeField] private WheelView wheelView;
        [SerializeField] private HudView hudView;
        [SerializeField] private RewardPopupView rewardPopup;
        [SerializeField] private BombExplodedView bombScreen;
        [SerializeField] private CashOutView cashOutScreen;
        [SerializeField] private GameOverView gameOverScreen;
        [SerializeField] private InventoryView inventoryView;
        [SerializeField] private AudioService audio;
        [SerializeField] private ScreenShake shake;

        private GameContext _ctx;
        private GameStateMachine _fsm;
        private KeyboardInput _keyboard;
        private readonly List<IDisposable> _presenters = new List<IDisposable>();

        private void Awake()
        {
            _ctx = new GameContext(tuning, settings);
            _fsm = new GameStateMachine();
            _ctx.SpinAnimator = index => wheelView.SpinTo(index, _ctx.CurrentWheel.SliceCount);

            var events = _ctx.Events;
            var input = new StateInput(_fsm, PlayClick);

            _presenters.Add(new WheelPresenter(events, input, wheelView));
            _presenters.Add(new HudPresenter(events, hudView));
            var inventory = new InventoryPresenter(events, input, hudView, inventoryView);
            _presenters.Add(inventory);
            _presenters.Add(new OverlayPresenter(events, input, rewardPopup, bombScreen, cashOutScreen, gameOverScreen));
            _presenters.Add(new FeedbackPresenter(events, audio, shake));

            _keyboard = new KeyboardInput(input, inventory);
        }

        private void Start() => _fsm.Change(new BootState(_ctx, _fsm));

        private void Update()
        {
            _fsm.Tick(Time.deltaTime);
            _keyboard.Tick();
        }

        private void OnDestroy()
        {
            foreach (var presenter in _presenters) presenter.Dispose();
            _presenters.Clear();
        }

        private void PlayClick()
        {
            if (audio != null) audio.PlayClick();
        }
    }
}
