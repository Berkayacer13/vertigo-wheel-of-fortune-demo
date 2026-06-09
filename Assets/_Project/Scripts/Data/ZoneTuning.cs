using UnityEngine;

namespace Wof.Data
{
    /// <summary>
    /// Global zone tuning: the safe/super intervals, the reward growth curve (R4) and
    /// the three wheel configs the builder selects between by zone type.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Zone Tuning", fileName = "zone_tuning")]
    public class ZoneTuning : ScriptableObject
    {
        [Min(1)] public int safeInterval = 5;
        [Min(1)] public int superInterval = 30;

        [Tooltip("Reward multiplier as a function of zone (R4: 'better every zone').")]
        public AnimationCurve rewardGrowth = AnimationCurve.Linear(1, 1f, 100, 10f);

        public WheelConfig normalWheel;
        public WheelConfig safeWheel;
        public WheelConfig superWheel;
    }
}
