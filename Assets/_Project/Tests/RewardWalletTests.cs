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
        public void Consume_hands_back_the_reward_it_removed()
        {
            // the caller needs the reward itself to announce its real id, so the out
            // overload must return the entry it took, not just say that it took one
            var w = new RewardWallet();
            w.Add(Gold(10u));
            w.Add(new Reward("reward_shield_prototype", RewardKind.Shield, 1u, "UI_Icons_Armor_Points"));

            Assert.IsTrue(w.TryConsume(RewardKind.Shield, out var consumed));
            Assert.AreEqual("reward_shield_prototype", consumed.Id);
            Assert.AreEqual(RewardKind.Shield, consumed.Kind);
        }

        [Test]
        public void Consume_reports_failure_without_a_reward()
        {
            var w = new RewardWallet();
            w.Add(Gold(10u));

            Assert.IsFalse(w.TryConsume(RewardKind.Shield, out var consumed));
            Assert.IsNull(consumed.Id, "nothing was removed, so there is no reward to report");
            Assert.AreEqual(1, w.RunRewards.Count);
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
