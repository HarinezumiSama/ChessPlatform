using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal struct GameMoveData
    {
        #region Constants and Fields

        private readonly GameMove _move;
        private readonly GameMoveInfo _moveInfo;

        #endregion

        #region Constructors

        internal GameMoveData([NotNull] GameMove move, GameMoveInfo moveInfo)
        {
            _move = move;
            _moveInfo = moveInfo;
        }

        #endregion

        #region Public Properties

        public GameMove Move
        {
            [DebuggerStepThrough]
            get
            {
                return _move;
            }
        }

        public GameMoveInfo MoveInfo
        {
            [DebuggerStepThrough]
            get
            {
                return _moveInfo;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} : {1}",
                this.Move,
                this.MoveInfo);
        }

        #endregion
    }
}