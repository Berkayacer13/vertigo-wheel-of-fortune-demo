using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class RewardRulesTests
    {
        [TestCase(WheelTier.Silver, true)]
        [TestCase(WheelTier.Bronze, false)]
        [TestCase(WheelTier.Golden, false)]
        public void Shield_is_a_silver_spin_gift(WheelTier tier, bool allowed)
            => Assert.AreEqual(allowed, RewardRules.CanAppearOn(RewardKind.Shield, tier));

        [TestCase(RewardKind.Gold)]
        [TestCase(RewardKind.Consumable)]
        [TestCase(RewardKind.SpecialSkin)]
        public void Other_rewards_are_not_restricted_by_tier(RewardKind kind)
        {
            foreach (WheelTier tier in System.Enum.GetValues(typeof(WheelTier)))
                Assert.IsTrue(RewardRules.CanAppearOn(kind, tier), $"{kind} on {tier}");
        }
    }
}
