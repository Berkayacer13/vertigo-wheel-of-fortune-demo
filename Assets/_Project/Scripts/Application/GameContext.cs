using System;
using System.Threading.Tasks;
using Wof.Data;
using Wof.Domain;

namespace Wof.Application
{
    /// <summary>
    /// Lite DI container: builds and owns every service once, exposes the current zone
    /// and wheel, and drives zone progression. States read everything they need from
    /// here, so they only ever take (ctx, fsm) — no long constructor chains.
    /// </summary>
    public sealed class GameContext
    {
        public GameEvents Events { get; }
        public ZoneTuning Tuning { get; }
        public GameSettings Settings { get; }
        public WheelBuilder Builder { get; }
        public EconomyService Economy { get; }
        public ISliceSelector Selector { get; }
        public RewardScaler Scaler { get; }

        /// <summary>
        /// View-supplied spin animation. Takes the pre-chosen landing index and returns
        /// a Task that completes when the wheel has visually landed. Set by the
        /// presentation layer; the SpinningState awaits it. Keeps Unity out of the logic.
        /// </summary>
        public Func<int, Task> SpinAnimator { get; set; } = _ => Task.CompletedTask;

        public int Zone { get; private set; } = 1;
        public WheelModel CurrentWheel { get; private set; }

        public GameContext(ZoneTuning tuning, GameSettings settings, ISliceSelector selector = null)
        {
            Tuning = tuning;
            Settings = settings;
            Events = new GameEvents();
            Builder = new WheelBuilder(tuning);
            Economy = new EconomyService(Events, settings.startingGold, settings.startingCash);
            Selector = selector ?? new WeightedSliceSelector();
            Scaler = new RewardScaler(z => tuning.rewardGrowth.Evaluate(z));
        }

        public void SetZone(int zone)
        {
            Zone = zone;
            CurrentWheel = Builder.BuildForZone(zone);
            var type = ZoneRules.Resolve(zone, Tuning.safeInterval, Tuning.superInterval);
            Events.RaiseZoneChanged(zone, type);
            Events.RaiseWheelBuilt(CurrentWheel);
        }

        public void NextZone() => SetZone(Zone + 1);

        public void ResetRun()
        {
            Economy.Wipe();
            SetZone(1);
        }
    }
}
