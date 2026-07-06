using OrbitalEscape.Movement;
using Zenject;

namespace OrbitalEscape.Cheats
{
    public sealed class MovementSpeedCheatInfoProvider : ICheatInfoProvider
    {
        private readonly IMovementSpeedService _movementSpeedService;

        public int Order => 0;

        [Inject]
        public MovementSpeedCheatInfoProvider(IMovementSpeedService movementSpeedService)
        {
            _movementSpeedService = movementSpeedService;
        }

        public string GetLine()
        {
            return $"Speed: {_movementSpeedService.CurrentLinearSpeed:0.00}";
        }
    }
}
