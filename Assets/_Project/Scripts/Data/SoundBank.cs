using UnityEngine;

namespace Wof.Data
{
    /// <summary>
    /// ScriptableObject SFX set — one clip per game moment, assigned in the editor
    /// (brief: proper use of ScriptableObjects). Swap clips without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Wof/Sound Bank", fileName = "sound_bank")]
    public sealed class SoundBank : ScriptableObject
    {
        public AudioClip click;
        public AudioClip spin;
        public AudioClip win;
        public AudioClip bomb;
        public AudioClip cashOut;
        [Range(0f, 1f)] public float volume = 0.8f;
    }
}
