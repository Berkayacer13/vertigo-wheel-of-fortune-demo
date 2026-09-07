using System.Collections.Generic;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Currency, permanent inventory, and run-wallet façade. Mutates state and fires the
    /// matching events so the HUD stays in sync without ever being called directly by logic.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly GameEvents _events;
        private readonly RewardWallet _wallet = new RewardWallet();

        public uint Gold { get; private set; }
        public uint Cash { get; private set; }
        public RewardWallet Wallet => _wallet;
        public PermanentRewardInventory PermanentInventory { get; } = new PermanentRewardInventory();

        public EconomyService(GameEvents events, uint startGold, uint startCash)
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
        /// (R10) Cash out: bank currency rewards into permanent balances, move item rewards
        /// into the permanent inventory, and return the full banked list for the cash-out screen.
        /// </summary>
        public IReadOnlyList<Reward> Bank()
        {
            var banked = _wallet.CashOut();
            foreach (var r in banked)
            {
                if (r.Kind == RewardKind.Gold) AddGold(ToCurrencyAmount(r));
                else if (r.Kind == RewardKind.Cash) AddCash(ToCurrencyAmount(r));
                else if (r.Kind != RewardKind.Bomb) PermanentInventory.Add(r);
            }
            _events.RaiseWalletChanged(0);
            return banked;
        }

        /// <summary>(R9) Spend gold on a revive; returns false if the player can't afford it.</summary>
        public bool TrySpendGold(uint cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            _events.RaiseCurrencyChanged(Gold, Cash);
            return true;
        }

        public void AddGold(uint amount)
        {
            Gold = checked(Gold + amount);
            _events.RaiseCurrencyChanged(Gold, Cash);
        }

        public void AddCash(uint amount)
        {
            Cash = checked(Cash + amount);
            _events.RaiseCurrencyChanged(Gold, Cash);
        }

        private static uint ToCurrencyAmount(Reward reward)
        {
            if (reward.Amount < 0)
                throw new System.ArgumentOutOfRangeException(nameof(reward), "Currency reward amount cannot be negative.");

            return checked((uint)reward.Amount);
        }
    }
}
