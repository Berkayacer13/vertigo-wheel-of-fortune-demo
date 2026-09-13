using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wof.Data;
using Wof.Domain;

namespace Wof.Tests
{
    /// <summary>
    /// Checks the wheels the game actually ships rather than a hand-built fixture: the rules
    /// can be right while the generated assets say otherwise.
    /// </summary>
    public class WheelAssetTests
    {
        private const string TuningPath = "Assets/_Project/Settings/zone_tuning.asset";

        private static ZoneTuning LoadTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<ZoneTuning>(TuningPath);
            Assert.IsNotNull(tuning, $"zone tuning missing at {TuningPath}");
            return tuning;
        }

        private static bool OffersShield(WheelConfig wheel) =>
            wheel.Slices.Any(s => !s.isBomb && s.reward != null && s.reward.Kind == RewardKind.Shield);

        [Test]
        public void Shield_is_offered_on_silver_spins_only()
        {
            var tuning = LoadTuning();
            Assert.IsTrue(OffersShield(tuning.safeWheel), "the silver wheel must offer the shield");
            Assert.IsFalse(OffersShield(tuning.normalWheel), "the bronze wheel must not offer the shield");
            Assert.IsFalse(OffersShield(tuning.superWheel), "the golden wheel must not offer the shield");
        }

        [Test]
        public void Every_shipped_slice_has_a_reward_its_wheel_may_carry()
        {
            var tuning = LoadTuning();
            foreach (var wheel in new[] { tuning.normalWheel, tuning.safeWheel, tuning.superWheel })
            {
                Assert.IsNotNull(wheel, "a zone type has no wheel assigned");
                for (int i = 0; i < wheel.Slices.Count; i++)
                {
                    var slice = wheel.Slices[i];
                    if (slice.isBomb) continue;
                    // a missing reward renders as an empty chamber that only shows up in play
                    Assert.IsNotNull(slice.reward, $"{wheel.name} slice {i} has no reward");
                    Assert.IsTrue(RewardRules.CanAppearOn(slice.reward.Kind, wheel.Tier),
                        $"{wheel.name} slice {i}: {slice.reward.name} may not appear on the {wheel.Tier} wheel");
                }
            }
        }
    }
}
