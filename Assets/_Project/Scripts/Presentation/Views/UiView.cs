using DG.Tweening;
using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Base for every View. Child references are auto-wired in OnValidate by name (brief:
    /// "Button references should be automatically set from OnValidate"), so nothing is
    /// dragged by hand and no OnClick is wired in the editor.
    /// <para>
    /// Also owns the shared overlay transition. Every full-screen screen in this game is a
    /// root GameObject that used to be snapped on and off with SetActive, which reads as a
    /// hard cut; <see cref="ShowRoot"/> / <see cref="HideRoot"/> fade the same root and pop
    /// its card instead, so all five overlays enter and leave the same way.
    /// </para>
    /// </summary>
    public abstract class UiView : MonoBehaviour
    {
        private const float FadeIn = 0.16f;
        private const float CardIn = 0.30f;
        private const float FadeOut = 0.11f;
        private const float CardFrom = 0.86f;   // card scales up into place from here

        private CanvasGroup _overlayGroup;
        private Sequence _overlaySeq;
        private bool _overlayVisible;

        /// <summary>Find a descendant by name and grab a component into <paramref name="field"/> if unset.</summary>
        protected T Bind<T>(ref T field, string childName) where T : Component
        {
            if (field == null)
            {
                var t = transform.FindDeep(childName);
                if (t != null) field = t.GetComponent<T>();
            }
            return field;
        }

        /// <summary>
        /// Reveal an overlay root: fade the whole screen in and spring its card up to size.
        /// </summary>
        /// <param name="card">Optional panel that pops; the dim background just fades.</param>
        /// <param name="delay">
        /// Hold the screen invisible this long before fading in. The root still activates
        /// immediately, so it swallows taps from the first frame — the delay only buys the
        /// player a beat to see what the wheel actually landed on before the screen covers it.
        /// </param>
        protected void ShowRoot(GameObject root, RectTransform card = null, float delay = 0f)
        {
            if (root == null) return;

            _overlaySeq?.Kill();
            root.SetActive(true);
            _overlayVisible = true;

            var group = OverlayGroup(root);
            group.alpha = 0f;
            group.blocksRaycasts = true;   // dead frames must not leak clicks to the wheel
            if (card != null) card.localScale = Vector3.one * CardFrom;

            // unscaled so a screen still opens if some future feedback pauses game time
            _overlaySeq = DOTween.Sequence().SetLink(root).SetUpdate(true).SetDelay(delay);
            _overlaySeq.Append(group.DOFade(1f, FadeIn));
            if (card != null)
                _overlaySeq.Join(card.DOScale(1f, CardIn).SetEase(Ease.OutBack));
            _overlaySeq.OnComplete(() => _overlaySeq = null);
        }

        /// <summary>
        /// Fade an overlay root out, then deactivate it. Cheap to call on an already-hidden
        /// screen: the GameController hides every overlay on each phase change, so this must
        /// no-op rather than start a fresh fade each time.
        /// </summary>
        protected void HideRoot(GameObject root)
        {
            if (root == null) return;

            if (!_overlayVisible)
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }

            _overlayVisible = false;
            _overlaySeq?.Kill();

            var group = OverlayGroup(root);
            group.blocksRaycasts = false;  // let the wheel take clicks while this fades out
            _overlaySeq = DOTween.Sequence().SetLink(root).SetUpdate(true)
                .Append(group.DOFade(0f, FadeOut))
                .OnComplete(() =>
                {
                    root.SetActive(false);
                    _overlaySeq = null;
                });
        }

        /// <summary>
        /// The CanvasGroup the fade runs on, added on demand so the scene builder does not
        /// have to remember one per screen.
        /// </summary>
        private CanvasGroup OverlayGroup(GameObject root)
        {
            if (_overlayGroup == null)
            {
                _overlayGroup = root.GetComponent<CanvasGroup>();
                if (_overlayGroup == null) _overlayGroup = root.AddComponent<CanvasGroup>();
            }
            return _overlayGroup;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => AutoWire();

        /// <summary>Each View binds its own children here.</summary>
        protected abstract void AutoWire();
#endif
    }
}
