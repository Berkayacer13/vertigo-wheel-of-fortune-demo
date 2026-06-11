using NUnit.Framework;
using Wof.Application;
using Wof.Domain;

namespace Wof.Tests
{
    /// <summary>
    /// Locks down the banking rules: run rewards live in the wallet (at risk) and only
    /// move into the permanent gold/cash balances when the player cashes out (R10).
    /// </summary>
    public class EconomyServiceTests
    {
        private static EconomyService NewEconomy(int gold = 100, int cash = 0)
            => new EconomyService(new GameEvents(), gold, cash);

        private static Reward Gold(int n) => new Reward("gold", RewardKind.Gold, n, "UI_icon_gold");
        private static Reward Cash(int n) => new Reward("cash", RewardKind.Cash, n, "UI_icon_cash");
        private static Reward Skin() => new Reward("skin", RewardKind.WeaponSkin, 1, "skin");

        [Test]
        public void Collecting_does_NOT_touch_balances_until_cashout()
        {
            var eco = NewEconomy(gold: 100);
            eco.AddRunReward(Gold(250));

            Assert.AreEqual(100, eco.Gold, "gold must stay at risk in the wallet until cash-out");
            Assert.AreEqual(1, eco.Wallet.RunRewards.Count);
        }

        [Test]
        public void Bank_credits_gold_and_cash_to_balances()
        {
            var eco = NewEconomy(gold: 100, cash: 5);
            eco.AddRunReward(Gold(250));
            eco.AddRunReward(Cash(15));
            eco.AddRunReward(Skin());

            var banked = eco.Bank();

            Assert.AreEqual(350, eco.Gold);
            Assert.AreEqual(20, eco.Cash);
            Assert.AreEqual(3, banked.Count, "full list returned for the cash-out screen");
            Assert.IsFalse(eco.Wallet.HasRewards);
        }

        [Test]
        public void Bomb_wipe_loses_wallet_but_keeps_balances()
        {
            var eco = NewEconomy(gold: 100);
            eco.AddRunReward(Gold(999));

            eco.Wipe();

            Assert.AreEqual(100, eco.Gold, "banked balance survives the bomb");
            Assert.IsFalse(eco.Wallet.HasRewards);
        }

        [Test]
        public void Revive_spends_gold_only_when_affordable()
        {
            var eco = NewEconomy(gold: 30);
            Assert.IsTrue(eco.TrySpendGold(25));
            Assert.AreEqual(5, eco.Gold);
            Assert.IsFalse(eco.TrySpendGold(25), "cannot afford a second revive");
            Assert.AreEqual(5, eco.Gold);
        }
    }
}
