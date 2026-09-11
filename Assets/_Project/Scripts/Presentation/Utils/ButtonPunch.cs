using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>
    /// Press feedback for every button: squash on touch-down, spring back on release.
    /// Unity's own Button only tints on press, which on a dark palette is almost invisible
    /// on a phone — under a thumb the colour change is literally covered by the thumb, while
    /// a scale change still reads at the edges.
    /// <para>
    /// The scale is restored in OnDisable because these live on overlays that get switched
    /// off mid-press; without it a button can come back squashed on its next screen.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const float PressedScale = 0.94f;
        private const float DownTime = 0.06f;
        private const float UpTime = 0.22f;

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable) return;   // a dead button must feel dead
            transform.DOKill();
            transform.DOScale(PressedScale, DownTime).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable) return;
            transform.DOKill();
            transform.DOScale(1f, UpTime).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}
