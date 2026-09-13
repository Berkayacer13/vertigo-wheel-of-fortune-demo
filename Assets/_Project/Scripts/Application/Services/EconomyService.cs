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
        /// <summary>
        /// How many bombs the player can currently survive. Counts wallet ENTRIES, not
        /// amounts, because <see cref="TryConsumeShield"/> removes one entry per revive —
        /// counting amounts would promise more revives than the wallet can actually pay.
        /// </summary>
        public int ShieldCount
        {
            get
            {
                int count = 0;
                foreach (var reward in _wallet.RunRewards)
                    if (reward.Kind == RewardKind.Shield) count++;
                return count;
            }
        }

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
        /// into the permanent inventory, and return a receipt of exactly what was paid.
        /// </summary>
        public BankReceipt Bank()
        {
            // Total everything up BEFORE emptying the wallet. The old order cashed the
            // wallet out first and credited inside the loop, so an overflow part-way
            // through threw with the wallet already cleared — the player lost the whole
            // run at the moment they tried to bank it. Nothing mutates until the
            // arithmetic below has proven it fits.
            uint goldGain = 0, cashGain = 0;
            foreach (var r in _wallet.RunRewards)
            {
                if (r.Kind == RewardKind.Gold) goldGain = checked(goldGain + r.Amount);
                else if (r.Kind == RewardKind.Cash) cashGain = checked(cashGain + r.Amount);
            }
            uint newGold = checked(Gold + goldGain);
            uint newCash = checked(Cash + cashGain);

            var banked = _wallet.CashOut();
            int items = 0;
            foreach (var r in banked)
            {
                if (r.Kind == RewardKind.Gold || r.Kind == RewardKind.Cash) continue;
                if (r.Kind == RewardKind.Bomb || r.Kind == RewardKind.Shield) continue;
                PermanentInventory.Add(r);
                items++;
            }

            Gold = newGold;
            Cash = newCash;
            _events.RaiseCurrencyChanged(Gold, Cash);
            _events.RaiseWalletChanged(0);
            return new BankReceipt(goldGain, cashGain, items, banked);
        }

        /// <summary>(R9) Spend gold on a revive; returns false if the player can't afford it.</summary>
        public bool TrySpendGold(uint cost)
        {
            if (Gold < cost) return false;
            Gold -= cost;
            _events.RaiseCurrencyChanged(Gold, Cash);
            return true;
        }

        /// <summary>Spend one shield to survive a bomb. The run wallet is otherwise untouched.</summary>
        public bool TryConsumeShield()
        {
            if (!_wallet.TryConsume(RewardKind.Shield, out var shield)) return false;

            // report the reward's own Id, so renaming the asset can never desync the HUD
            _events.RaiseRewardConsumed(shield.Id);
            _events.RaiseWalletChanged(_wallet.RunRewards.Count);
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

    }
}
