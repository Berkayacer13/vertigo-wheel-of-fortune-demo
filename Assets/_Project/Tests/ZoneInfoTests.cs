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

        [Test]
        public void Types_neighbouring_zones_with_its_own_intervals()
        {
            // the zone track draws the zones around the current one; they must follow the
            // same tuning the current zone was resolved with
            var info = ZoneInfo.For(1, safe: 4, super: 7);
            Assert.AreEqual(ZoneType.Safe, info.TypeAt(4));
            Assert.AreEqual(ZoneType.Super, info.TypeAt(7));
            Assert.AreEqual(ZoneType.Normal, info.TypeAt(5));
            Assert.AreEqual(ZoneType.Safe, ZoneInfo.For(1).TypeAt(5), "defaults still resolve x5 as safe");
        }
    }
}
