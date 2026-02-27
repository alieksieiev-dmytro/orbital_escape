#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace OrbitalEscape.Editor
{
    [InitializeOnLoad]
    public sealed class SceneBootstrapper
    {
        private const string BOOTSTRAP_SCENE_PATH = "Assets/Scenes/Launcher.unity";

        static SceneBootstrapper()
        {
            //EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            if (EditorSceneManager.GetActiveScene().path == BOOTSTRAP_SCENE_PATH)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            EditorSceneManager.OpenScene(BOOTSTRAP_SCENE_PATH);
        }
    }
}
#endif