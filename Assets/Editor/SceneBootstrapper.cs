#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace OrbitalEscape
{
    [InitializeOnLoad]
    public sealed class SceneBootstrapper
    {
        private const string BootstrapScenePath = "Assets/Scenes/Launcher.unity";

        static SceneBootstrapper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            if (EditorSceneManager.GetActiveScene().path == BootstrapScenePath)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            EditorSceneManager.OpenScene(BootstrapScenePath);
        }
    }
}
#endif