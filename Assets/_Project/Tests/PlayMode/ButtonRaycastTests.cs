using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Wof.Tests.PlayMode
{
    /// <summary>
    /// Verifies every button is PHYSICALLY clickable: a pointer raycast at the button's
    /// center must hit the button itself (not some dim/overlay/label above it).
    /// onClick.Invoke() in the smoke tests bypasses raycasting, so this is the test
    /// that catches "click eaten by another graphic" bugs.
    /// </summary>
    public class ButtonRaycastTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            SceneManager.LoadScene(0);
            yield return null;
            yield return null;
        }

        private static GameObject FindAny(string name)
        {
            foreach (var t in Object.FindObjectsOfType<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        private static GameObject TopHitAt(GameObject button)
        {
            var pos = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            var ped = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);
            return results.Count > 0 ? results[0].gameObject : null;
        }

        private static void AssertClickable(string buttonName)
        {
            var button = FindAny(buttonName);
            Assert.IsNotNull(button, $"{buttonName} not found");
            var top = TopHitAt(button);
            Assert.IsNotNull(top, $"{buttonName}: nothing hit at its position");
            Assert.IsTrue(top == button || top.transform.IsChildOf(button.transform),
                $"{buttonName}: click would hit '{top.name}' instead");
        }

        [UnityTest]
        public IEnumerator Wheel_buttons_are_clickable_in_idle()
        {
            AssertClickable("ui_button_spin");
            AssertClickable("ui_button_leave");
            AssertClickable("ui_button_inventory");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Overlay_buttons_are_clickable_when_their_screen_is_open()
        {
            var overlays = new Dictionary<string, string[]>
            {
                ["ui_popup_reward"] = new[] { "ui_button_collect" },
                ["ui_screen_bomb"] = new[] { "ui_button_giveup", "ui_button_revive_gold", "ui_button_revive_ad" },
                ["ui_screen_cashout"] = new[] { "ui_button_cashout_confirm" },
                ["ui_screen_gameover"] = new[] { "ui_button_restart" },
                ["ui_screen_inventory"] = new[] { "ui_button_inventory_close" },
            };

            foreach (var pair in overlays)
            {
                var overlay = FindAny(pair.Key);
                overlay.SetActive(true);
                yield return null;

                foreach (var buttonName in pair.Value) AssertClickable(buttonName);

                overlay.SetActive(false);
                yield return null;
            }
        }
    }
}
