using System;
using System.Collections.Generic;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Lightweight typed event hub — the one-way logic -> UI channel. Presenters subscribe;
    /// the logic layer never holds a reference to a View. Payloads carry everything the
    /// screen needs, so nothing on the UI side has to reach back into the rules, the
    /// economy or the config assets to finish drawing. This is what keeps the UI decoupled
    /// from the rules.
    /// </summary>
    public sealed class GameEvents
    {
        public event Action<ZoneInfo> ZoneChanged;
        public event Action<WheelModel> WheelBuilt;
        public event Action SpinStarted;
        public event Action<int> SpinLandedOnIndex;
        public event Action<Reward> RewardWon;
        public event Action<uint, int> BombExploded;        // revive gold cost, shields held
        public event Action<IReadOnlyList<Reward>> RewardsBanked;   // cash-out: the collected list
        public event Action<int> WalletChanged;             // run reward count
        public event Action<string> RewardConsumed;         // run reward removed by a gameplay effect
        public event Action<uint, uint> CurrencyChanged;    // gold, cash
        public event Action<GamePhase> PhaseChanged;

        public void RaiseZoneChanged(ZoneInfo zone) => ZoneChanged?.Invoke(zone);
        public void RaiseWheelBuilt(WheelModel wheel) => WheelBuilt?.Invoke(wheel);
        public void RaiseSpinStarted() => SpinStarted?.Invoke();
        public void RaiseSpinLanded(int index) => SpinLandedOnIndex?.Invoke(index);
        public void RaiseRewardWon(Reward reward) => RewardWon?.Invoke(reward);
        public void RaiseBombExploded(uint reviveGoldCost, int shieldCount) => BombExploded?.Invoke(reviveGoldCost, shieldCount);
        public void RaiseRewardsBanked(IReadOnlyList<Reward> banked) => RewardsBanked?.Invoke(banked);
        public void RaiseWalletChanged(int runCount) => WalletChanged?.Invoke(runCount);
        public void RaiseRewardConsumed(string rewardId) => RewardConsumed?.Invoke(rewardId);
        public void RaiseCurrencyChanged(uint gold, uint cash) => CurrencyChanged?.Invoke(gold, cash);
        public void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);
    }
}
