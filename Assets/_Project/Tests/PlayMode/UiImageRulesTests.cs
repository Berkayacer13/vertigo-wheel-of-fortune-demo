using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Wof.Tests.PlayMode
{
    /// <summary>
    /// The brief's Image rules, checked on the real scene after boot rather than trusted to
    /// the scene builder: sprites are never stretched (sliced with a border, or aspect
    /// preserved), and Raycast Target / Maskable stay off unless the image has to take input.
    /// A zone-track redesign once shipped two stretched sprites without any test noticing.
    /// </summary>
    public class UiImageRulesTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            SceneManager.LoadScene(0);
            yield return null; // Awake/Start
            yield return null; // BootState -> Idle, slices rendered
        }

        [UnityTest]
        public IEnumerator Sprites_are_sliced_or_keep_their_aspect()
        {
            var offenders = new List<string>();
            foreach (var image in Object.FindObjectsOfType<Image>(true))
            {
                if (image.sprite == null || image.preserveAspect) continue;
                bool sliced = image.type == Image.Type.Sliced && image.sprite.border != Vector4.zero;
                if (!sliced)
                    offenders.Add($"{PathOf(image.transform)} ({image.type}, sprite {image.sprite.name}, border {image.sprite.border})");
            }

            Assert.IsEmpty(offenders, "stretched sprites:\n" + string.Join("\n", offenders));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Only_input_images_are_raycast_targets_and_none_are_maskable()
        {
            var offenders = new List<string>();
            foreach (var image in Object.FindObjectsOfType<Image>(true))
            {
                if (image.maskable)
                    offenders.Add($"{PathOf(image.transform)} is maskable");

                // a button's body must take clicks; the overlay dim must swallow them
                bool takesInput = IsButtonGraphic(image) || image.name == "ui_image_overlay_dim";
                if (image.raycastTarget && !takesInput)
                    offenders.Add($"{PathOf(image.transform)} is a raycast target");
            }

            Assert.IsEmpty(offenders, string.Join("\n", offenders));
            yield return null;
        }

        private static bool IsButtonGraphic(Image image)
        {
            for (var t = image.transform; t != null; t = t.parent)
            {
                var button = t.GetComponent<Button>();
                if (button != null && button.targetGraphic == image) return true;
            }
            return false;
        }

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
