using System;
using Wof.Application;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>The top bar: balances, the run's stake, and the zone track.</summary>
    public sealed class HudPresenter : IDisposable
    {
        private readonly GameEvents _events;
        private readonly HudView _view;

        public HudPresenter(GameEvents events, HudView view)
        {
            _events = events;
            _view = view;

            _events.CurrencyChanged += _view.SetCurrency;
            // Drive AT RISK from the wallet, not from the inventory grid: the grid merges
            // repeat wins into one cell, so counting its cells froze the readout at 1 while
            // the player kept stacking rewards they could lose.
            _events.WalletChanged += _view.SetRunCount;
            _events.ZoneChanged += OnZoneChanged;
        }

        public void Dispose()
        {
            _events.CurrencyChanged -= _view.SetCurrency;
            _events.WalletChanged -= _view.SetRunCount;
            _events.ZoneChanged -= OnZoneChanged;
        }

        private void OnZoneChanged(ZoneInfo zone) => _view.SetZone(zone.Zone, zone.Type);
    }
}
