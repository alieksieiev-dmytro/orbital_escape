using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class JumpCommand : ICommand
    {
        private JumpBehaviour _jumpBehaviour; 
        
        [Inject]
        public void Construct(JumpBehaviour jumpBehaviour) => _jumpBehaviour = jumpBehaviour;
        
        public void Execute() => _jumpBehaviour.Jump();
    }
}