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
        [SerializeField] private InventoryView inventoryView;
        [SerializeField] private AudioService audio;

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
            e.SpinStarted += OnSpinStarted;
            e.RewardWon += OnRewardWon;
            e.BombExploded += OnBombExploded;
            e.RewardsBanked += OnRewardsBanked;
            e.CurrencyChanged += hudView.SetCurrency;
            e.WalletChanged += OnWalletChanged;
            e.PhaseChanged += OnPhaseChanged;
        }

        private void Unsubscribe()
        {
            if (_ctx == null) return;
            var e = _ctx.Events;
            e.WheelBuilt -= wheelView.Render;
            e.ZoneChanged -= OnZoneChanged;
            e.SpinStarted -= OnSpinStarted;
            e.RewardWon -= OnRewardWon;
            e.BombExploded -= OnBombExploded;
            e.RewardsBanked -= OnRewardsBanked;
            e.CurrencyChanged -= hudView.SetCurrency;
            e.WalletChanged -= OnWalletChanged;
            e.PhaseChanged -= OnPhaseChanged;
        }

        private void OnSpinStarted() => Sfx(a => a.PlaySpin());

        private void OnRewardWon(Reward reward)
        {
            // on a safe/super zone the popup also offers "leave & collect" so the player can
            // bank the just-won silver/golden reward and walk away without re-entering risk
            rewardPopup.Show(reward, _canLeaveCurrentZone);
            inventoryView.AddItem(reward);
            Sfx(a => a.PlayWin());
        }

        private void OnRewardsBanked(System.Collections.Generic.IReadOnlyList<Reward> banked)
        {
            cashOutScreen.Show(banked);
            Sfx(a => a.PlayCashOut());
        }

        private void OnWalletChanged(int runCount)
        {
            hudView.SetRunCount(runCount);
            if (runCount == 0) inventoryView.Clear(); // cash-out or bomb give-up
        }

        private void OnZoneChanged(int zone, ZoneType type)
        {
            wheelView.SetTitle(TitleFor(type));
            hudView.SetZone(zone, type);
            _canLeaveCurrentZone = ZoneRules.CanLeave(zone, tuning.safeInterval, tuning.superInterval);
            wheelView.SetLeaveEnabled(false); // re-enabled once we settle into Idle
        }

        private void OnBombExploded()
        {
            bombScreen.Show(_ctx.Settings.reviveGoldCost);
            Sfx(a => a.PlayBomb());
        }

        private void Sfx(System.Action<AudioService> play) { if (audio != null) play(audio); }

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
            inventoryView.Hide();
        }

        // ---- UI -> logic (forwarded to whatever state accepts it) -----------

        private void BindViewInputs()
        {
            wheelView.BindInput(onSpin: () => Press<ISpinInput>(s => s.OnSpin()),
                                onLeave: () => Press<ISpinInput>(s => s.OnLeave()));
            rewardPopup.BindCollect(() => Press<ICollectInput>(s => s.OnCollect()));
            rewardPopup.BindLeave(() => Press<ICollectInput>(s => s.OnCollectAndLeave()));
            bombScreen.BindInput(
                onReviveGold: () => Press<IReviveInput>(s => s.OnReviveGold()),
                onReviveAd: () => Press<IReviveInput>(s => s.OnReviveAd()),
                onGiveUp: () => Press<IReviveInput>(s => s.OnGiveUp()));
            cashOutScreen.BindConfirm(() => Press<ICashOutInput>(s => s.OnConfirm()));
            gameOverScreen.BindRestart(() => Press<IRestartInput>(s => s.OnRestart()));

            // inventory is a read-only viewer — pure presentation, no game rule involved,
            // so it's wired view-to-view instead of through the state machine
            hudView.BindInventory(() => { Sfx(a => a.PlayClick()); inventoryView.Show(); });
            inventoryView.BindClose(() => { Sfx(a => a.PlayClick()); inventoryView.Hide(); });
        }

        /// <summary>Plays the click SFX, then routes the input to the active state.</summary>
        private void Press<T>(System.Action<T> action) where T : class
        {
            Sfx(a => a.PlayClick());
            Forward(action);
        }

        /// <summary>Routes a player input to the active state only if it accepts that input.</summary>
        private void Forward<T>(System.Action<T> action) where T : class
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
