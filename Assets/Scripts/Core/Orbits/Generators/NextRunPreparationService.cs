using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class NextRunPreparationService : IDisposable
    {
        private readonly ILevelBuilder _builder;
        private readonly LevelGenerationConfig _config;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private UniTaskCompletionSource<LevelBuildResult> _pending;

        public NextRunPreparationService(ILevelBuilder builder, LevelGenerationConfig config)
        {
            _builder = builder;
            _config = config;
        }

        public void Prepare()
        {
            if (_pending != null || _lifetime.IsCancellationRequested)
                return;

            _pending = new UniTaskCompletionSource<LevelBuildResult>();
            PrepareAsync(_pending, _lifetime.Token).Forget();
        }

        public async UniTask<LevelBuildResult> TakeAsync(CancellationToken cancellationToken)
        {
            Prepare();
            var pending = _pending;
            try
            {
                return await pending.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (ReferenceEquals(_pending, pending))
                    _pending = null;
            }
        }

        private async UniTask PrepareAsync(
            UniTaskCompletionSource<LevelBuildResult> pending, CancellationToken token)
        {
            try
            {
                pending.TrySetResult(await _builder.BuildAsync(Array.Empty<Orbit>(), _config.InitialOrbitCount, token));
            }
            catch (OperationCanceledException)
            {
                pending.TrySetCanceled(token);
            }
            catch (Exception exception)
            {
                pending.TrySetException(exception);
            }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
