using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wof.Presentation;

namespace Wof.Tests.PlayMode
{
    /// <summary>
    /// End-to-end smoke: loads the real Game scene, presses SPIN through the actual
    /// Button, waits for the DOTween spin to finish and asserts the game resolved to
    /// either a reward popup or the bomb screen. Verifies the whole wiring chain:
    /// controller -> FSM -> events -> views.
    /// </summary>
    public class GameSceneSmokeTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            SceneManager.LoadScene(0);
            yield return null; // Awake/Start
            yield return null; // BootState -> Idle
        }

        private static Button FindButton(string name)
        {
            foreach (var b in Object.FindObjectsOfType<Button>(true))
                if (b.name == name) return b;
            return null;
        }

        private static GameObject FindAny(string name)
        {
            foreach (var t in Object.FindObjectsOfType<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        [UnityTest]
        public IEnumerator Boot_lands_in_zone1_idle_with_spin_enabled()
        {
            var zone = FindAny("ui_text_zone_value").GetComponent<TMP_Text>();
            Assert.AreEqual("ZONE 1", zone.text);

            var spin = FindButton("ui_button_spin");
            Assert.IsNotNull(spin, "spin button missing");
            Assert.IsTrue(spin.interactable, "spin should be enabled in Idle");

            var leave = FindButton("ui_button_leave");
            Assert.IsFalse(leave.interactable, "leave must be disabled on a normal zone");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spin_resolves_to_reward_or_bomb()
        {
            var spin = FindButton("ui_button_spin");
            spin.onClick.Invoke();
            yield return null;
            Assert.IsFalse(spin.interactable, "spin must lock while spinning");

            // spinDuration is 3.5s — wait it out plus margin
            yield return new WaitForSeconds(5.5f);

            bool popup = FindAny("ui_popup_reward").activeInHierarchy;
            bool bomb = FindAny("ui_screen_bomb").activeInHierarchy;
            Assert.IsTrue(popup || bomb, "after a spin either the reward popup or the bomb screen must show");

            if (popup)
            {
                FindButton("ui_button_collect").onClick.Invoke();
                yield return null;
                var zone = FindAny("ui_text_zone_value").GetComponent<TMP_Text>();
                Assert.AreEqual("ZONE 2", zone.text, "collect should advance to zone 2");
            }
            else
            {
                // bomb on zone 1: give up ends the run on the game-over screen
                FindButton("ui_button_giveup").onClick.Invoke();
                yield return null;
                Assert.IsTrue(FindAny("ui_screen_gameover").activeInHierarchy);
            }
        }

        [UnityTest]
        public IEnumerator Wheel_view_renders_eight_slices()
        {
            var wheel = Object.FindObjectOfType<WheelView>();
            Assert.IsNotNull(wheel);
            var slices = wheel.GetComponentsInChildren<SliceView>(true);
            Assert.AreEqual(8, slices.Length);
            yield return null;
        }
    }
}
