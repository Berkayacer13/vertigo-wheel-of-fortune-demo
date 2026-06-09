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
    }
}
