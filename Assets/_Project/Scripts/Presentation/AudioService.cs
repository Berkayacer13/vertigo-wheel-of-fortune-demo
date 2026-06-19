using UnityEngine;
using Wof.Data;

namespace Wof.Presentation
{
    /// <summary>
    /// Plays one-shot SFX from a <see cref="SoundBank"/>. Driven by the GameController off
    /// the event bus — no PlayOnAwake and no editor OnClick wiring (brief). Every call is
    /// null-safe, so the game runs fine with an empty or partial bank.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private SoundBank bank;
        [SerializeField] private AudioSource source;

        public void PlayClick() => Play(bank != null ? bank.click : null);
        public void PlaySpin() => Play(bank != null ? bank.spin : null);
        public void PlayWin() => Play(bank != null ? bank.win : null);
        public void PlayBomb() => Play(bank != null ? bank.bomb : null);
        public void PlayCashOut() => Play(bank != null ? bank.cashOut : null);

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null)
                source.PlayOneShot(clip, bank != null ? bank.volume : 1f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (source == null) source = GetComponent<AudioSource>();
        }
#endif
    }
}
