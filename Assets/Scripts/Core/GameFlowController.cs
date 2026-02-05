using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrbitalEscape
{
    public class GameFlowController : MonoBehaviour
    {
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
            SceneManager.LoadScene(Constants.SCENE_GAMEPLAY);
        }
    }
}
