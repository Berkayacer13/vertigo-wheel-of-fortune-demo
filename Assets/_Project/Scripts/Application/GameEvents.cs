using System;
using System.Collections.Generic;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Lightweight typed event hub — the one-way logic -> UI channel. Views subscribe
    /// in OnEnable and unsubscribe in OnDisable; the logic layer never holds a
    /// reference to a View. This is what keeps the UI decoupled from the rules.
    /// </summary>
    public sealed class GameEvents
    {
        public event Action<int, ZoneType> ZoneChanged;     // zone, type
        public event Action<WheelModel> WheelBuilt;
        public event Action SpinStarted;
        public event Action<int> SpinLandedOnIndex;
        public event Action<Reward> RewardWon;
        public event Action BombExploded;
        public event Action<IReadOnlyList<Reward>> RewardsBanked;   // cash-out: the collected list
        public event Action<int> WalletChanged;             // run reward count
        public event Action<uint, uint> CurrencyChanged;    // gold, cash
        public event Action<GamePhase> PhaseChanged;

        public void RaiseZoneChanged(int zone, ZoneType type) => ZoneChanged?.Invoke(zone, type);
        public void RaiseWheelBuilt(WheelModel wheel) => WheelBuilt?.Invoke(wheel);
        public void RaiseSpinStarted() => SpinStarted?.Invoke();
        public void RaiseSpinLanded(int index) => SpinLandedOnIndex?.Invoke(index);
        public void RaiseRewardWon(Reward reward) => RewardWon?.Invoke(reward);
        public void RaiseBombExploded() => BombExploded?.Invoke();
        public void RaiseRewardsBanked(IReadOnlyList<Reward> banked) => RewardsBanked?.Invoke(banked);
        public void RaiseWalletChanged(int runCount) => WalletChanged?.Invoke(runCount);
        public void RaiseCurrencyChanged(uint gold, uint cash) => CurrencyChanged?.Invoke(gold, cash);
        public void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);
    }
}
