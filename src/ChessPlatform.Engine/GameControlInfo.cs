using System;
using System.Runtime.CompilerServices;
using System.Threading;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal sealed class GameControlInfo
    {
        private Func<Exception> _customInterruptionFactory;

        public GameControlInfo([NotNull] IGameControl gameControl, CancellationToken cancellationToken)
        {
            GameControl = gameControl.EnsureNotNull();
            CancellationToken = cancellationToken;
        }

        public IGameControl GameControl
        {
            get;
        }

        public CancellationToken CancellationToken
        {
            get;
        }

        public bool IsMoveNowAllowed
        {
            get;
            private set;
        }

        public bool IsCustomInterruptionRequested => _customInterruptionFactory != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AllowMoveNow()
        {
            IsMoveNowAllowed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestCustomInterruption<TException>()
            where TException : Exception, new()
        {
            static Exception CreateException() => new TException();
            Interlocked.CompareExchange(ref _customInterruptionFactory, CreateException, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckInterruptions()
        {
            CancellationToken.ThrowIfCancellationRequested();

            var customInterruptionException = _customInterruptionFactory?.Invoke();
            if (customInterruptionException != null)
            {
                throw customInterruptionException;
            }

            if (IsMoveNowAllowed)
            {
                GameControl.ThrowIfMoveNowIsRequested();
            }
        }
    }
}