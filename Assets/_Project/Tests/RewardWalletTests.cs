using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class RewardWalletTests
    {
        private static Reward Gold(int amount) => new Reward("gold", RewardKind.Gold, amount, "ui_icon_gold");

        [Test]
        public void Accumulates_rewards()
        {
            var w = new RewardWallet();
            w.Add(Gold(10));
            w.Add(Gold(5));
            Assert.IsTrue(w.HasRewards);
            Assert.AreEqual(2, w.RunRewards.Count);
        }

        [Test]
        public void Bomb_wipes_all()
        {
            var w = new RewardWallet();
            w.Add(Gold(10));
            w.DetonateBomb();
            Assert.IsFalse(w.HasRewards);
        }

        [Test]
        public void CashOut_returns_then_clears()
        {
            var w = new RewardWallet();
            w.Add(Gold(10));
            w.Add(Gold(20));

            var banked = w.CashOut();

            Assert.AreEqual(2, banked.Count);
            Assert.IsFalse(w.HasRewards, "wallet should be empty after cashing out");
        }
    }
}
