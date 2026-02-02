namespace OrbitalEscape.Input
{
    public sealed class RestartCommand : ICommand
    {
        private string _sceneName;

        public RestartCommand(string sceneName)
        {
            _sceneName = sceneName;
        }
        
        public void Execute()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneName);
        }
    }
}
