using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrbitalEscape
{
    public class GameFlowController : MonoBehaviour
    {
        private void Start() => SceneManager.LoadScene("Gameplay", LoadSceneMode.Additive);
    }
}
