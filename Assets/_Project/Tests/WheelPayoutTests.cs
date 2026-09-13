using NUnit.Framework;
using UnityEditor;
using Wof.Application;
using Wof.Data;
using Wof.Domain;

namespace Wof.Tests
{
    /// <summary>
    /// End to end over the shipped tuning: the amount printed under a chamber must be the
    /// amount the player is paid when the wheel lands on it. From zone 2 on they used to
    /// differ, because the chamber showed the base amount and landing scaled it afterwards.
    /// </summary>
    public class WheelPayoutTests
    {
        private static GameContext NewContext()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<ZoneTuning>("Assets/_Project/Settings/zone_tuning.asset");
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/_Project/Settings/game_settings.asset");
            Assert.IsNotNull(tuning, "zone tuning asset missing");
            Assert.IsNotNull(settings, "game settings asset missing");
            return new GameContext(tuning, settings);
        }

        [TestCase(2)]    // first zone where the growth curve moves off 1.0
        [TestCase(7)]    // bronze, further along the curve
        [TestCase(10)]   // silver
        [TestCase(30)]   // golden
        public void Landing_pays_exactly_what_the_chamber_shows(int zone)
        {
            var ctx = NewContext();
            var fsm = new GameStateMachine();
            ctx.SetZone(zone);

            Reward? paid = null;
            ctx.Events.RewardWon += r => paid = r;

            for (int i = 0; i < ctx.CurrentWheel.SliceCount; i++)
            {
                var shown = ctx.CurrentWheel.SliceAt(i);
                if (shown.IsBomb) continue;

                paid = null;
                fsm.Change(new ResolvingState(ctx, fsm, i));

                Assert.IsTrue(paid.HasValue, $"zone {zone} slice {i} paid nothing");
                Assert.AreEqual(shown.Reward.Id, paid.Value.Id);
                Assert.AreEqual(shown.Reward.Amount, paid.Value.Amount,
                    $"zone {zone}: the chamber showed x{shown.Reward.Amount} of {shown.Reward.Id}");
            }
        }

        [Test]
        public void Chambers_show_the_zone_growth_not_the_base_amount()
        {
            // the other half of the guarantee: equal numbers must not come from both the
            // chamber and the payout falling back to the base amount
            var early = NewContext();
            early.SetZone(1);
            var later = NewContext();
            later.SetZone(7);

            Assert.Greater(GoldShown(later), GoldShown(early));
        }

        private static uint GoldShown(GameContext ctx)
        {
            foreach (var slice in ctx.CurrentWheel.Slices)
                if (!slice.IsBomb && slice.Reward.Kind == RewardKind.Gold)
                    return slice.Reward.Amount;
            Assert.Fail("the bronze wheel has no gold chamber");
            return 0;
        }
    }
}
