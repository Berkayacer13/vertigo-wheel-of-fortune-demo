using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class RewardWalletTests
    {
        private static Reward Gold(uint amount) => new Reward("gold", RewardKind.Gold, amount, "ui_icon_gold");

        [Test]
        public void Accumulates_rewards()
        {
            var w = new RewardWallet();
            w.Add(Gold(10u));
            w.Add(Gold(5u));
            Assert.IsTrue(w.HasRewards);
            Assert.AreEqual(2, w.RunRewards.Count);
        }

        [Test]
        public void Bomb_wipes_all()
        {
            var w = new RewardWallet();
            w.Add(Gold(10u));
            w.DetonateBomb();
            Assert.IsFalse(w.HasRewards);
        }

        [Test]
        public void CashOut_returns_then_clears()
        {
            var w = new RewardWallet();
            w.Add(Gold(10u));
            w.Add(Gold(20u));

            var banked = w.CashOut();

            Assert.AreEqual(2, banked.Count);
            Assert.IsFalse(w.HasRewards, "wallet should be empty after cashing out");
        }

        [Test]
        public void Consume_removes_only_the_requested_reward()
        {
            var w = new RewardWallet();
            w.Add(new Reward("shield", RewardKind.Shield, 1u, "UI_Icons_Armor_Points"));
            w.Add(Gold(10u));

            Assert.IsTrue(w.TryConsume(RewardKind.Shield));
            Assert.AreEqual(1, w.RunRewards.Count);
            Assert.AreEqual(RewardKind.Gold, w.RunRewards[0].Kind);
            Assert.IsFalse(w.TryConsume(RewardKind.Shield));
        }
    }
}
