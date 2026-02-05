using System;
using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Input
{
    public sealed class JumpCommand : Command
    {
        public override IConvertible Type => CommandType.Jump;
        
        private readonly JumpBehaviour _jumpBehaviour; 
        
        [Inject]
        public JumpCommand(JumpBehaviour jumpBehaviour) => _jumpBehaviour = jumpBehaviour;
        public override void Execute() => _jumpBehaviour.Jump();
    }
}