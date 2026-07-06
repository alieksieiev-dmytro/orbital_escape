using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Events;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Score;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Cheats
{
    public sealed class CheatCommandService : ICheatCommandService
    {
        private const float CameraRadiusPadding = 2.0f;

        private readonly CheatPanelConfig _config;
        private readonly IScoreService _scoreService;
        private readonly LevelManager _levelManager;
        private readonly LevelState _levelState;
        private readonly PlayerOrbitState _orbitState;
        private readonly IMovementModel _movementModel;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;
        private readonly IGravityWellInfluenceService _gravityWellInfluenceService;
        private readonly PlayerMovementController _movementController;

        [Inject]
        public CheatCommandService(
            CheatPanelConfig config,
            IScoreService scoreService,
            LevelManager levelManager,
            LevelState levelState,
            PlayerOrbitState orbitState,
            IMovementModel movementModel,
            IOrbitJumpTransitionService jumpTransitionService,
            IGravityWellInfluenceService gravityWellInfluenceService,
            PlayerMovementController movementController)
        {
            _config = config;
            _scoreService = scoreService;
            _levelManager = levelManager;
            _levelState = levelState;
            _orbitState = orbitState;
            _movementModel = movementModel;
            _jumpTransitionService = jumpTransitionService;
            _gravityWellInfluenceService = gravityWellInfluenceService;
            _movementController = movementController;
        }

        public CheatCommandResult AddScore()
        {
            if (!_config.CheatsEnabled)
            {
                return new CheatCommandResult(false, "Cheats are disabled.");
            }

            _scoreService.UpdateScore(1.0f);
            return new CheatCommandResult(true, "Score +1");
        }

        public CheatCommandResult ClearAllObstacles()
        {
            if (!_config.CheatsEnabled)
            {
                return new CheatCommandResult(false, "Cheats are disabled.");
            }

            _levelManager.ClearObstacleContent();
            _gravityWellInfluenceService.ClearState();
            return new CheatCommandResult(true, "Obstacles cleared.");
        }

        public async UniTask<CheatCommandResult> GoToOrbitAsync(int orbitIndex, CancellationToken cancellationToken)
        {
            if (!_config.CheatsEnabled)
            {
                return new CheatCommandResult(false, "Cheats are disabled.");
            }

            if (orbitIndex < 0)
            {
                return new CheatCommandResult(false, "Orbit index must be 0 or higher.");
            }

            int requiredOrbitIndex = orbitIndex + 1;
            await _levelManager.EnsureOrbitAvailableAsync(requiredOrbitIndex, cancellationToken);

            if (orbitIndex < 0 || requiredOrbitIndex >= _levelState.Count)
            {
                return new CheatCommandResult(false, $"Orbit {orbitIndex} is not available.");
            }

            Orbit currentOrbit = _levelState.GetOrbit(orbitIndex);
            Orbit oppositeOrbit = _levelState.GetOrbit(requiredOrbitIndex);
            float targetRadius = _orbitState.GetMovementRadius(currentOrbit, oppositeOrbit);

            _jumpTransitionService.Stop();
            _gravityWellInfluenceService.ClearState();
            _orbitState.SetCurrentAndOpposite(currentOrbit, oppositeOrbit);
            _movementModel.Radius = targetRadius;
            _movementController.RefreshTransform();
            EventBus<OrbitsSetEvent>.Raise(new OrbitsSetEvent(currentOrbit, oppositeOrbit));
            EventBus<OrbitFocusRequestedEvent>.Raise(new OrbitFocusRequestedEvent(targetRadius + CameraRadiusPadding));

            return new CheatCommandResult(true, $"Moved to orbit {orbitIndex}.");
        }
    }
}
