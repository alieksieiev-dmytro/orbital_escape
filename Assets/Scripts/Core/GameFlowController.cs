using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrbitalEscape
{
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameFlowConfig _config;

        private void Start()
        {
            var scene = gameObject.scene;

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                if (root == gameObject)
                    continue;

                DontDestroyOnLoad(root);
            }

            DontDestroyOnLoad(gameObject);
            SceneManager.LoadScene(GetNextSceneName());
        }

        private string GetNextSceneName()
        {
            if (_config == null)
            {
                Debug.LogWarning($"{nameof(GameFlowController)} has no {nameof(GameFlowConfig)} assigned. Loading gameplay directly.");
                return Constants.SCENE_GAMEPLAY;
            }

            return _config.SkipMainMenu
                ? Constants.SCENE_GAMEPLAY
                : Constants.SCENE_MAIN_MENU;
        }
    }
}
