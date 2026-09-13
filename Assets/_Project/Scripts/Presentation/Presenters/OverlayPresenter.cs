using System;
using Wof.Application;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// The modal screens that interrupt the wheel — reward, bomb, cash-out and game over:
    /// which one is up for the current phase, what each one shows, and what its buttons mean.
    /// </summary>
    public sealed class OverlayPresenter : IDisposable
    {
        private readonly GameEvents _events;
        private readonly RewardPopupView _rewardPopup;
        private readonly BombExplodedView _bombScreen;
        private readonly CashOutView _cashOutScreen;
        private readonly GameOverView _gameOverScreen;
        private bool _canLeave;

        public OverlayPresenter(GameEvents events, StateInput input, RewardPopupView rewardPopup,
            BombExplodedView bombScreen, CashOutView cashOutScreen, GameOverView gameOverScreen)
        {
            _events = events;
            _rewardPopup = rewardPopup;
            _bombScreen = bombScreen;
            _cashOutScreen = cashOutScreen;
            _gameOverScreen = gameOverScreen;

            _rewardPopup.BindCollect(() => input.Press<ICollectInput>(s => s.OnCollect()));
            _rewardPopup.BindLeave(() => input.Press<ICollectInput>(s => s.OnCollectAndLeave()));
            _bombScreen.BindInput(
                onReviveGold: () => input.Press<IReviveInput>(s => s.OnReviveGold()),
                onReviveFree: () => input.Press<IReviveInput>(s => s.OnReviveFree()),
                onGiveUp: () => input.Press<IReviveInput>(s => s.OnGiveUp()));
            _cashOutScreen.BindConfirm(() => input.Press<ICashOutInput>(s => s.OnConfirm()));
            _gameOverScreen.BindRestart(() => input.Press<IRestartInput>(s => s.OnRestart()));

            _events.ZoneChanged += OnZoneChanged;
            _events.PhaseChanged += OnPhaseChanged;
            _events.RewardWon += OnRewardWon;
            _events.BombExploded += _bombScreen.Show;
            _events.RewardsBanked += _cashOutScreen.Show;
        }

        public void Dispose()
        {
            _events.ZoneChanged -= OnZoneChanged;
            _events.PhaseChanged -= OnPhaseChanged;
            _events.RewardWon -= OnRewardWon;
            _events.BombExploded -= _bombScreen.Show;
            _events.RewardsBanked -= _cashOutScreen.Show;
        }

        private void OnZoneChanged(ZoneInfo zone) => _canLeave = zone.CanLeave;

        // on a safe/super zone the popup also offers "leave & collect" so the player can
        // bank the just-won silver/golden reward and walk away without re-entering risk
        private void OnRewardWon(Reward reward) => _rewardPopup.Show(reward, _canLeave);

        private void OnPhaseChanged(GamePhase phase)
        {
            if (!OverlayPolicy.ClearsOverlays(phase)) return;

            _rewardPopup.Hide();
            _bombScreen.Hide();
            _cashOutScreen.Hide();
            _gameOverScreen.Hide();
            if (phase == GamePhase.GameOver) _gameOverScreen.Show();
        }
    }
}
