using System.Collections.Generic;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Currency + wallet façade. Mutates state and fires the matching events so the HUD
    /// stays in sync without ever being called directly by the logic.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly GameEvents _events;
        private readonly RewardWallet _wallet = new RewardWallet();

        public int Gold { get; private set; }
        public int Cash { get; private set; }
        public RewardWallet Wallet => _wallet;

        public EconomyService(GameEvents events, int startGold, int startCash)
        {
            _events = events;
            Gold = startGold;
            Cash = startCash;
        }

        public void AddRunReward(Reward r)
        {
            _wallet.Add(r);
            _events.RaiseWalletChanged(_wallet.RunRewards.Count);
        }

        /// <summary>(R8) Bomb: wipe the run rewards.</summary>
        public void Wipe()
        {
            _wallet.DetonateBomb();
            _events.RaiseWalletChanged(0);
        }

        /// <summary>
        /// (R10) Cash out: bank currency rewards into permanent balances and return the
        /// full banked list so the cash-out screen can display what was collected.
        /// </summary>
        public IReadOnlyList<Reward> Bank()
        {
            var banked = _wallet.CashOut();
            foreach (var r in banked)
            {
                if (r.Kind == RewardKind.Gold) AddGold(r.Amount);
                else if (r.Kind == RewardKind.Cash) AddCash(r.Amount);
            }
            _events.RaiseWalletChanged(0);
            return banked;
        }

        /// <summary>(R9) Spend gold on a revive; returns false if the player can't afford it.</summary>
        public bool TrySpendGold(int cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            _events.RaiseCurrencyChanged(Gold, Cash);
            return true;
        }

        public void AddGold(int n) { Gold += n; _events.RaiseCurrencyChanged(Gold, Cash); }
        public void AddCash(int n) { Cash += n; _events.RaiseCurrencyChanged(Gold, Cash); }
    }
}
