using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Wof.EditorTools
{
    /// <summary>
    /// Captures the Game view at the brief's three aspect ratios (20:9, 16:9, 4:3).
    /// Entering play mode reloads the domain, so the request is parked in SessionState
    /// and picked up again by the [InitializeOnLoad] constructor after the reload.
    /// Run from the menu, or headful CLI via
    /// -executeMethod Wof.EditorTools.ScreenshotTool.CaptureAll (GUI editor required —
    /// ScreenCapture cannot render under -nographics).
    /// </summary>
    [InitializeOnLoad]
    public static class ScreenshotTool
    {
        private static readonly (string name, int w, int h)[] Sizes =
        {
            ("20x9", 1080, 2400),
            ("16x9", 1080, 1920),
            ("4x3", 1080, 1440),
        };

        private const string PendingKey = "wof_capture_pending";
        private const string QuitKey = "wof_capture_quit";
        private const string OutDir = "docs/screenshots";

        private static int _index = -1;
        private static int _wait;
        private static bool _shotRequested;

        static ScreenshotTool()
        {
            // resumes the capture after the play-mode domain reload
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PendingKey, false) || !UnityEngine.Application.isPlaying) return;
                SessionState.SetBool(PendingKey, false);
                StartLoop();
            };
        }

        [MenuItem("Wof/Capture Aspect Screenshots")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutDir);
            SessionState.SetBool(QuitKey,
                Environment.GetCommandLineArgs().Any(a => a == "-executeMethod"));

            if (EditorApplication.isPlaying)
            {
                StartLoop();
                return;
            }

            // make sure we're capturing the actual game, not whatever scene was open
            const string scenePath = "Assets/_Project/Scenes/Game.unity";
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath)
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true; // -> domain reload -> static ctor resumes
        }

        private static void StartLoop()
        {
            _index = -1;
            NextSize();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (_wait-- > 0) return;

            if (!_shotRequested)
            {
                string file = $"{OutDir}/wof_{Sizes[_index].name}.png";
                ScreenCapture.CaptureScreenshot(file);
                Debug.Log($"[Screenshot] {file}");
                _shotRequested = true;
                _wait = 30; // let the async write land
                return;
            }

            if (_index + 1 < Sizes.Length)
            {
                NextSize();
                return;
            }

            EditorApplication.update -= Tick;
            if (SessionState.GetBool(QuitKey, false)) EditorApplication.Exit(0);
            else EditorApplication.isPlaying = false;
        }

        private static void NextSize()
        {
            _index++;
            var (label, w, h) = Sizes[_index];
            SetGameViewSize(w, h, label);
            _shotRequested = false;
            _wait = 45; // settle frames after the resize
        }

        // ---- GameView size via reflection (no public API for this) ----------

        private static void SetGameViewSize(int width, int height, string label)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var groupEnum = asm.GetType("UnityEditor.GameViewSizeGroupType");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleType.GetProperty("instance").GetValue(null);

            // use the size group of the ACTIVE build target so the selection sticks
            object groupType = sizesType.GetProperty("currentGroupType").GetValue(instance);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });

            var getTotal = group.GetType().GetMethod("GetTotalCount");
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            int total = (int)getTotal.Invoke(group, null);
            int found = -1;
            for (int i = 0; i < total; i++)
            {
                var s = getSize.Invoke(group, new object[] { i });
                int sw = (int)s.GetType().GetProperty("width").GetValue(s);
                int sh = (int)s.GetType().GetProperty("height").GetValue(s);
                if (sw == width && sh == height) { found = i; break; }
            }

            if (found < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var sizeKindEnum = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(
                    new[] { sizeKindEnum, typeof(int), typeof(int), typeof(string) });
                var newSize = ctor.Invoke(new object[]
                    { Enum.Parse(sizeKindEnum, "FixedResolution"), width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                found = (int)getTotal.Invoke(group, null) - 1;
            }

            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(window, found);
            window.Repaint();
        }
    }
}
