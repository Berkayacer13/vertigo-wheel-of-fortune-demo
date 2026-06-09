using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wof.Presentation
{
    /// <summary>End-of-run screen with a single Restart action (R8).</summary>
    public sealed class GameOverView : UiView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleValue;     // ui_text_gameover_value
        [SerializeField] private Button restartButton;    // ui_button_restart

        private Action _onRestart;

        public void BindRestart(Action onRestart) => _onRestart = onRestart;

        private void OnEnable() { if (restartButton != null) restartButton.onClick.AddListener(HandleRestart); }
        private void OnDisable() { if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart); }
        private void HandleRestart() => _onRestart?.Invoke();

        public void Show() { if (root != null) root.SetActive(true); }
        public void Hide() { if (root != null) root.SetActive(false); }

#if UNITY_EDITOR
        protected override void AutoWire()
        {
            Bind(ref titleValue, "ui_text_gameover_value");
            Bind(ref restartButton, "ui_button_restart");
            if (root == null) root = gameObject;
        }
#endif
    }
}
