using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine.SmartEnough
{
    [DebuggerDisplay("[{GetType().Name,nq}] Primary = {Primary}, Secondary = {Secondary}")]
    internal struct KillerMoveData
    {
        #region Public Properties

        [CanBeNull]
        public GameMove Primary
        {
            get;
            private set;
        }

        [CanBeNull]
        public GameMove Secondary
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void RecordKiller([NotNull] GameMove killerMove)
        {
            #region Argument Check

            if (killerMove == null)
            {
                throw new ArgumentNullException(nameof(killerMove));
            }

            #endregion

            if (Primary == null)
            {
                Primary = killerMove;
            }
            else if (killerMove != Primary)
            {
                Secondary = killerMove;
            }
        }

        #endregion
    }
}