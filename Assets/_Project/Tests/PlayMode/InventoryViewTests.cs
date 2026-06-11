using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wof.Domain;
using Wof.Presentation;

namespace Wof.Tests.PlayMode
{
    /// <summary>Inventory (run stash) view: items appear per reward and wipe with the wallet.</summary>
    public class InventoryViewTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            SceneManager.LoadScene(0);
            yield return null;
            yield return null;
        }

        private static Reward Gold(int amount) =>
            new Reward("gold", RewardKind.Gold, amount, "UI_icon_gold");

        private static Reward Grenade() =>
            new Reward("grenade", RewardKind.Consumable, 2, "ui_icon_render_cons_grenade_m26");

        [UnityTest]
        public IEnumerator Same_reward_stacks_into_one_cell()
        {
            var inv = Object.FindObjectOfType<InventoryView>(true);
            Assert.IsNotNull(inv, "InventoryView missing from scene");

            inv.AddItem(Gold(10));
            inv.AddItem(Gold(25));   // same id -> stacks, amounts sum
            inv.AddItem(Grenade());  // different id -> new cell

            Assert.AreEqual(2, inv.ItemCount, "gold x2 must stack into a single cell");

            inv.Clear();
            Assert.AreEqual(0, inv.ItemCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Show_displays_empty_hint_only_when_empty()
        {
            var inv = Object.FindObjectOfType<InventoryView>(true);
            inv.Show();
            yield return null;

            var hint = GameObject.Find("ui_text_inventory_empty_value");
            Assert.IsNotNull(hint, "empty hint should be visible when nothing collected");

            inv.AddItem(Gold(5));
            yield return null;
            Assert.IsNull(GameObject.Find("ui_text_inventory_empty_value"),
                "hint must hide once an item exists"); // Find only sees active objects

            inv.Hide();
        }
    }
}
