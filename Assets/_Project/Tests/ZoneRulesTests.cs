using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class ZoneRulesTests
    {
        [TestCase(1, ZoneType.Normal)]
        [TestCase(4, ZoneType.Normal)]
        [TestCase(5, ZoneType.Safe)]
        [TestCase(10, ZoneType.Safe)]
        [TestCase(25, ZoneType.Safe)]
        [TestCase(30, ZoneType.Super)]   // the trap: x30 is Super, NOT Safe
        [TestCase(60, ZoneType.Super)]
        [TestCase(7, ZoneType.Normal)]
        [TestCase(0, ZoneType.Normal)]
        [TestCase(-3, ZoneType.Normal)]
        public void Resolves_zone_type(int zone, ZoneType expected)
            => Assert.AreEqual(expected, ZoneRules.Resolve(zone));

        [Test] public void Normal_has_bomb() => Assert.IsTrue(ZoneRules.HasBomb(3));
        [Test] public void Safe_has_no_bomb() => Assert.IsFalse(ZoneRules.HasBomb(5));
        [Test] public void Super_has_no_bomb() => Assert.IsFalse(ZoneRules.HasBomb(30));

        [Test] public void Cannot_leave_normal() => Assert.IsFalse(ZoneRules.CanLeave(4));
        [Test] public void Can_leave_safe() => Assert.IsTrue(ZoneRules.CanLeave(5));
        [Test] public void Can_leave_super() => Assert.IsTrue(ZoneRules.CanLeave(30));

        [TestCase(ZoneType.Normal, WheelTier.Bronze)]
        [TestCase(ZoneType.Safe, WheelTier.Silver)]
        [TestCase(ZoneType.Super, WheelTier.Golden)]
        public void Maps_type_to_tier(ZoneType type, WheelTier expected)
            => Assert.AreEqual(expected, ZoneRules.TierFor(type));

        [Test]
        public void Honors_custom_intervals()
        {
            Assert.AreEqual(ZoneType.Safe, ZoneRules.Resolve(3, safeInterval: 3, superInterval: 12));
            Assert.AreEqual(ZoneType.Super, ZoneRules.Resolve(12, safeInterval: 3, superInterval: 12));
        }

        [TestCase(1, 4)]
        [TestCase(4, 1)]
        [TestCase(5, 0)]    // already safe — nothing to wait for
        [TestCase(6, 4)]
        [TestCase(29, 1)]
        [TestCase(30, 0)]   // already super
        [TestCase(31, 4)]
        public void Counts_zones_until_the_player_may_leave(int zone, int expected)
            => Assert.AreEqual(expected, ZoneRules.ZonesUntilLeave(zone));

        [Test]
        public void Countdown_reaches_zero_exactly_when_leaving_is_allowed()
        {
            for (int zone = 1; zone <= 120; zone++)
                Assert.AreEqual(ZoneRules.CanLeave(zone), ZoneRules.ZonesUntilLeave(zone) == 0,
                    $"countdown disagrees with CanLeave at zone {zone}");
        }

        [Test]
        public void Countdown_takes_whichever_milestone_lands_first()
        {
            // super (7) is not a multiple of safe (4), so zone 5 must count to 7, not 8
            Assert.AreEqual(2, ZoneRules.ZonesUntilLeave(5, safe: 4, super: 7));
            Assert.AreEqual(1, ZoneRules.ZonesUntilLeave(3, safe: 4, super: 7));
        }
    }
}
