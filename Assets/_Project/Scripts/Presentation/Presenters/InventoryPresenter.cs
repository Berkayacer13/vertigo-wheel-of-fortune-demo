using System;
using Wof.Application;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// The run stash: keeps its contents in step with the wallet, and opens and closes it.
    /// The stash is a read-only viewer with no game rule behind it, so opening it is a click,
    /// not a state input.
    /// </summary>
    public sealed class InventoryPresenter : IDisposable
    {
        private readonly GameEvents _events;
        private readonly StateInput _input;
        private readonly InventoryView _view;

        public InventoryPresenter(GameEvents events, StateInput input, HudView hud, InventoryView view)
        {
            _events = events;
            _input = input;
            _view = view;

            hud.BindInventory(() => { _input.Click(); _view.Show(); });
            _view.BindClose(() => { _input.Click(); _view.Hide(); });

            _events.RewardWon += _view.AddItem;
            _events.RewardConsumed += OnRewardConsumed;
            _events.WalletChanged += OnWalletChanged;
            _events.PhaseChanged += OnPhaseChanged;
        }

        public void Dispose()
        {
            _events.RewardWon -= _view.AddItem;
            _events.RewardConsumed -= OnRewardConsumed;
            _events.WalletChanged -= OnWalletChanged;
            _events.PhaseChanged -= OnPhaseChanged;
        }

        /// <summary>Close the stash if it is showing. True when the press was used up doing so.</summary>
        public bool CloseIfOpen()
        {
            if (!_view.IsOpen) return false;
            _input.Click();
            _view.Hide();
            return true;
        }

        private void OnRewardConsumed(string rewardId) => _view.RemoveItem(rewardId);

        private void OnWalletChanged(int runCount)
        {
            if (runCount == 0) _view.Clear(); // cash-out or bomb give-up
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (OverlayPolicy.ClearsOverlays(phase)) _view.Hide();
        }
    }
}
