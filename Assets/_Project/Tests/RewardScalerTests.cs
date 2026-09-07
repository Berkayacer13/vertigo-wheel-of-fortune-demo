using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class RewardScalerTests
    {
        [Test]
        public void Scales_amount_by_zone_multiplier()
        {
            var scaler = new RewardScaler(zone => zone); // multiplier == zone number
            var baseReward = new Reward("gold", RewardKind.Gold, 10, "ui_icon_gold");

            Assert.AreEqual(10, scaler.Scale(baseReward, 1).Amount);
            Assert.AreEqual(30, scaler.Scale(baseReward, 3).Amount);
        }

        [Test]
        public void Rewards_are_monotonic_across_zones()
        {
            var scaler = new RewardScaler(zone => 1f + 0.5f * zone);
            var baseReward = new Reward("cash", RewardKind.Cash, 8, "ui_icon_cash");

            uint prev = 0;
            for (int zone = 1; zone <= 30; zone++)
            {
                uint amount = scaler.Scale(baseReward, zone).Amount;
                Assert.That(amount, Is.GreaterThanOrEqualTo(prev), $"zone {zone} should not pay less");
                prev = amount;
            }
        }

        [Test]
        public void Negative_multiplier_cannot_create_negative_reward()
        {
            var scaler = new RewardScaler(_ => -1f);
            var reward = new Reward("gold", RewardKind.Gold, 10, "ui_icon_gold");

            Assert.AreEqual(0u, scaler.Scale(reward, 1).Amount);
        }

        [Test]
        public void Bomb_is_never_scaled()
        {
            var scaler = new RewardScaler(zone => 100f);
            var bomb = new Reward("bomb", RewardKind.Bomb, 0, "ui_card_icon_death");

            var result = scaler.Scale(bomb, 10);

            Assert.IsTrue(result.IsBomb);
            Assert.AreEqual(0, result.Amount);
        }
    }
}
