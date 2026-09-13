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
    /// It scales the button's body — its target graphic, a child — and never the button's own
    /// transform: that one belongs to layout, and the brief keeps UI animation off root
    /// transforms. A button whose graphic sits on its root gets no squash rather than a root
    /// animation.
    /// </para>
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
        private Transform _body;

        private void Awake()
        {
            _button = GetComponent<Button>();
            var graphic = _button.targetGraphic;
            _body = graphic != null && graphic.transform != transform ? graphic.transform : null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_body == null || !_button.interactable) return;   // a dead button must feel dead
            _body.DOKill();
            _body.DOScale(PressedScale, DownTime).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_body == null || !_button.interactable) return;
            _body.DOKill();
            _body.DOScale(1f, UpTime).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            if (_body == null) return;
            _body.DOKill();
            _body.localScale = Vector3.one;
        }
    }
}
