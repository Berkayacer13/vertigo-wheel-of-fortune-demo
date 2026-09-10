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
        private readonly Random _shuffleRandom;

        public GameContext(ZoneTuning tuning, GameSettings settings, ISliceSelector selector = null,
            Random shuffleRandom = null)
        {
            Tuning = tuning;
            Settings = settings;
            Events = new GameEvents();
            Builder = new WheelBuilder(tuning);
            Economy = new EconomyService(Events, settings.startingGold, settings.startingCash);
            Selector = selector ?? new WeightedSliceSelector();
            Scaler = new RewardScaler(z => tuning.rewardGrowth.Evaluate(z));
            _shuffleRandom = shuffleRandom ?? new Random();
        }

        public void SetZone(int zone)
        {
            Zone = zone;
            // shuffle before announcing, so a zone's first spin is already randomised
            // and the View still renders the wheel exactly once per zone
            CurrentWheel = Builder.BuildForZone(zone).Shuffled(_shuffleRandom);
            var type = ZoneRules.Resolve(zone, Tuning.safeInterval, Tuning.superInterval);
            Events.RaiseZoneChanged(zone, type);
            Events.RaiseWheelBuilt(CurrentWheel);
        }

        public void NextZone() => SetZone(Zone + 1);

        /// <summary>
        /// Re-orders the current wheel and tells the View to redraw. Call this only while an
        /// overlay covers the wheel: redrawing a visible wheel makes the slice icons jump.
        /// </summary>
        public void ShuffleCurrentWheel()
        {
            CurrentWheel = CurrentWheel.Shuffled(_shuffleRandom);
            Events.RaiseWheelBuilt(CurrentWheel);
        }

        public void ResetRun()
        {
            Economy.Wipe();
            SetZone(1);
        }
    }
}
