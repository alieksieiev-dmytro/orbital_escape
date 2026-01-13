using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class JumpCommand : ICommand
    {
        private readonly PlayerMovementController _playerMovementController;

        private bool _isUpperOrbit = false;
        
        [Inject]
        public JumpCommand(PlayerMovementController playerMovementController)
        {
            _playerMovementController = playerMovementController;
        }
        
        public void Execute()
        {
            _isUpperOrbit = !_isUpperOrbit;
            _playerMovementController.SetRadius(_isUpperOrbit ? 20.0f : 15.0f);
        }
    }
}