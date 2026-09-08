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
        private static EconomyService NewEconomy(uint gold = 100, uint cash = 0)
            => new EconomyService(new GameEvents(), gold, cash);

        private static Reward Gold(uint amount) => new Reward("gold", RewardKind.Gold, amount, "UI_icon_gold");
        private static Reward Cash(uint amount) => new Reward("cash", RewardKind.Cash, amount, "UI_icon_cash");
        private static Reward Skin() => new Reward("skin", RewardKind.WeaponSkin, 1, "skin");
        private static Reward Chest(uint amount) => new Reward("chest", RewardKind.Chest, amount, "chest");
        private static Reward Bomb() => new Reward("bomb", RewardKind.Bomb, 1, "bomb");

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
        public void Bank_moves_item_rewards_into_permanent_inventory_and_stacks_them()
        {
            var eco = NewEconomy();
            eco.AddRunReward(Skin());
            eco.AddRunReward(Skin());
            eco.AddRunReward(Chest(2));

            eco.Bank();

            Assert.AreEqual(2, eco.PermanentInventory.Count);
            Assert.IsTrue(eco.PermanentInventory.TryGet("skin", out var skin));
            Assert.AreEqual(2, skin.Amount);
            Assert.IsTrue(eco.PermanentInventory.TryGet("chest", out var chest));
            Assert.AreEqual(2, chest.Amount);
        }

        [Test]
        public void Bank_never_adds_bomb_to_permanent_inventory()
        {
            var eco = NewEconomy();
            eco.AddRunReward(Bomb());

            eco.Bank();

            Assert.AreEqual(0, eco.PermanentInventory.Count);
        }

        [Test]
        public void Bank_does_not_make_shield_permanent()
        {
            var eco = NewEconomy();
            eco.AddRunReward(new Reward("shield", RewardKind.Shield, 1u, "UI_Icons_Armor_Points"));

            eco.Bank();

            Assert.AreEqual(0, eco.PermanentInventory.Count);
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

        [Test]
        public void Shield_is_consumed_without_wiping_the_run()
        {
            var eco = NewEconomy();
            eco.AddRunReward(new Reward("shield", RewardKind.Shield, 1u, "UI_Icons_Armor_Points"));
            eco.AddRunReward(Gold(250));

            Assert.IsTrue(eco.TryConsumeShield());
            Assert.AreEqual(0, eco.ShieldCount);
            Assert.AreEqual(1, eco.Wallet.RunRewards.Count);
            Assert.IsFalse(eco.TryConsumeShield());
        }

        [Test]
        public void Currency_addition_throws_instead_of_wrapping_on_overflow()
        {
            var eco = NewEconomy();
            eco.AddGold(uint.MaxValue - eco.Gold);

            Assert.Throws<System.OverflowException>(() => eco.AddGold(1));
        }

    }
}
