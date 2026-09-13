using System;
using System.Collections.Generic;
using Wof.Application;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// Sound and screen shake for game events. Both are optional — the game runs silent and
    /// still with an empty sound bank or no shake root — so every call is null-checked with
    /// Unity's own null, never <c>?.</c>, which would miss a destroyed component.
    /// </summary>
    public sealed class FeedbackPresenter : IDisposable
    {
        private readonly GameEvents _events;
        private readonly AudioService _audio;
        private readonly ScreenShake _shake;

        public FeedbackPresenter(GameEvents events, AudioService audio, ScreenShake shake)
        {
            _events = events;
            _audio = audio;
            _shake = shake;

            _events.SpinStarted += OnSpinStarted;
            _events.RewardWon += OnRewardWon;
            _events.RewardsBanked += OnRewardsBanked;
            _events.BombExploded += OnBombExploded;
        }

        public void Dispose()
        {
            _events.SpinStarted -= OnSpinStarted;
            _events.RewardWon -= OnRewardWon;
            _events.RewardsBanked -= OnRewardsBanked;
            _events.BombExploded -= OnBombExploded;
        }

        private void OnSpinStarted() { if (_audio != null) _audio.PlaySpin(); }

        private void OnRewardWon(Reward reward) { if (_audio != null) _audio.PlayWin(); }

        private void OnRewardsBanked(IReadOnlyList<Reward> banked) { if (_audio != null) _audio.PlayCashOut(); }

        private void OnBombExploded(uint reviveGoldCost, int shieldCount)
        {
            if (_audio != null) _audio.PlayBomb();
            // full trauma: losing the run is the single biggest event in the game, and the
            // shake is what the player feels before they have read a word of the screen
            if (_shake != null) _shake.AddTrauma(1f);
        }
    }
}
