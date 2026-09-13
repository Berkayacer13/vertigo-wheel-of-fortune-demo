using NUnit.Framework;
using Wof.Domain;

namespace Wof.Tests
{
    public class ZoneInfoTests
    {
        [Test]
        public void Normal_zone_is_locked_and_counts_down()
        {
            var info = ZoneInfo.For(7);
            Assert.AreEqual(7, info.Zone);
            Assert.AreEqual(ZoneType.Normal, info.Type);
            Assert.IsFalse(info.CanLeave);
            Assert.AreEqual(3, info.ZonesUntilLeave);
        }

        [TestCase(5, ZoneType.Safe)]
        [TestCase(30, ZoneType.Super)]
        public void Milestone_zone_can_be_left_right_now(int zone, ZoneType type)
        {
            var info = ZoneInfo.For(zone);
            Assert.AreEqual(type, info.Type);
            Assert.IsTrue(info.CanLeave);
            Assert.AreEqual(0, info.ZonesUntilLeave);
        }

        [Test]
        public void Honors_custom_intervals()
        {
            var ahead = ZoneInfo.For(3, safe: 4, super: 7);
            Assert.AreEqual(ZoneType.Normal, ahead.Type);
            Assert.AreEqual(1, ahead.ZonesUntilLeave);

            var super = ZoneInfo.For(7, safe: 4, super: 7);
            Assert.AreEqual(ZoneType.Super, super.Type);
            Assert.IsTrue(super.CanLeave);
        }
    }
}
