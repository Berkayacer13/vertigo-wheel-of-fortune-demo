using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Wof.EditorTools
{
    /// <summary>
    /// Headless batch runs leave the "last opened scene" blank, so the editor greets
    /// you with an empty Untitled scene. If that happens, open the Game scene instead.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenGameSceneOnLoad
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        static OpenGameSceneOnLoad()
        {
            if (UnityEngine.Application.isBatchMode) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                var active = EditorSceneManager.GetActiveScene();
                bool untitled = string.IsNullOrEmpty(active.path);
                if (untitled && !active.isDirty && File.Exists(ScenePath))
                    EditorSceneManager.OpenScene(ScenePath);
            };
        }
    }
}
