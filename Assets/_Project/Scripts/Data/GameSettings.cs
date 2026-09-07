using UnityEngine;

namespace Wof.Data
{
    /// <summary>
    /// Tunable game settings (spin feel + economy). The spin easing is an
    /// <see cref="AnimationCurve"/> rather than a DOTween Ease so the Data layer stays
    /// free of any tween-library dependency; the View feeds it to DOTween's
    /// SetEase(AnimationCurve) overload.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Game Settings", fileName = "game_settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Spin")]
        [Min(0.1f)] public float spinDuration = 3.5f;
        [Min(1)] public int fullRotations = 5;                  // whole turns before landing
        [Tooltip("Spin easing; should decelerate (fast -> slow) so the wheel 'clicks' under the indicator.")]
        public AnimationCurve spinEase = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));                      // ease-out

        [Header("Economy")]
        [Min(0)] public uint startingGold = 100;
        [Min(0)] public uint reviveGoldCost = 25;               // matches the brief screenshot
        [Min(0)] public uint startingCash = 0;
    }
}
