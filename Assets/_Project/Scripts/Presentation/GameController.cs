using UnityEngine;
using Wof.Application;
using Wof.Data;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// The only MonoBehaviour that owns the game logic. Wires logic -> UI (event bus) and
    /// UI -> logic (input interfaces forwarded to the active state), and drives the FSM.
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

        private GameContext _ctx;
        private GameStateMachine _fsm;
        private bool _canLeaveCurrentZone;

        private void Awake()
        {
            _ctx = new GameContext(tuning, settings);
            _fsm = new GameStateMachine();
            _ctx.SpinAnimator = index => wheelView.SpinTo(index, _ctx.CurrentWheel.SliceCount);

            Subscribe();
            BindViewInputs();
        }

        private void Start() => _fsm.Change(new BootState(_ctx, _fsm));

        private void Update() => _fsm.Tick(Time.deltaTime);

        private void OnDestroy() => Unsubscribe();

        // ---- logic -> UI ----------------------------------------------------

        private void Subscribe()
        {
            var e = _ctx.Events;
            e.WheelBuilt += wheelView.Render;
            e.ZoneChanged += OnZoneChanged;
            e.RewardWon += rewardPopup.Show;
            e.BombExploded += OnBombExploded;
            e.RewardsBanked += cashOutScreen.Show;
            e.CurrencyChanged += hudView.SetCurrency;
            e.WalletChanged += hudView.SetRunCount;
            e.PhaseChanged += OnPhaseChanged;
        }

        private void Unsubscribe()
        {
            if (_ctx == null) return;
            var e = _ctx.Events;
            e.WheelBuilt -= wheelView.Render;
            e.ZoneChanged -= OnZoneChanged;
            e.RewardWon -= rewardPopup.Show;
            e.BombExploded -= OnBombExploded;
            e.RewardsBanked -= cashOutScreen.Show;
            e.CurrencyChanged -= hudView.SetCurrency;
            e.WalletChanged -= hudView.SetRunCount;
            e.PhaseChanged -= OnPhaseChanged;
        }

        private void OnZoneChanged(int zone, ZoneType type)
        {
            wheelView.SetTitle(TitleFor(type));
            hudView.SetZone(zone, type);
            _canLeaveCurrentZone = ZoneRules.CanLeave(zone, tuning.safeInterval, tuning.superInterval);
            wheelView.SetLeaveEnabled(false); // re-enabled once we settle into Idle
        }

        private void OnBombExploded() => bombScreen.Show(_ctx.Settings.reviveGoldCost);

        private void OnPhaseChanged(GamePhase phase)
        {
            bool idle = phase == GamePhase.Idle;
            wheelView.SetSpinEnabled(idle);
            wheelView.SetLeaveEnabled(idle && _canLeaveCurrentZone);

            switch (phase)
            {
                case GamePhase.Boot:
                case GamePhase.ZoneIntro:
                case GamePhase.Idle:
                case GamePhase.Spinning:
                case GamePhase.Resolving:
                    HideOverlays();
                    break;
                case GamePhase.GameOver:
                    HideOverlays();
                    gameOverScreen.Show();
                    break;
                // Reward / BombExploded / CashOut overlays are shown by their data events.
            }
        }

        private void HideOverlays()
        {
            rewardPopup.Hide();
            bombScreen.Hide();
            cashOutScreen.Hide();
            gameOverScreen.Hide();
        }

        // ---- UI -> logic (forwarded to whatever state accepts it) -----------

        private void BindViewInputs()
        {
            wheelView.BindInput(onSpin: () => Input<ISpinInput>(s => s.OnSpin()),
                                onLeave: () => Input<ISpinInput>(s => s.OnLeave()));
            rewardPopup.BindCollect(() => Input<ICollectInput>(s => s.OnCollect()));
            bombScreen.BindInput(
                onReviveGold: () => Input<IReviveInput>(s => s.OnReviveGold()),
                onReviveAd: () => Input<IReviveInput>(s => s.OnReviveAd()),
                onGiveUp: () => Input<IReviveInput>(s => s.OnGiveUp()));
            cashOutScreen.BindConfirm(() => Input<ICashOutInput>(s => s.OnConfirm()));
            gameOverScreen.BindRestart(() => Input<IRestartInput>(s => s.OnRestart()));
        }

        private void Input<T>(System.Action<T> action) where T : class
        {
            if (_fsm.Current is T input) action(input);
        }

        private static string TitleFor(ZoneType type) => type switch
        {
            ZoneType.Super => "GOLDEN SPIN",
            ZoneType.Safe => "SILVER SPIN",
            _ => "SPIN",
        };
    }
}
