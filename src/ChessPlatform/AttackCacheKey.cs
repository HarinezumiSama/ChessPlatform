using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    internal struct AttackCacheKey
    {
        #region Constants and Fields

        private readonly Position _position;
        private readonly Piece _piece;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AttackCacheKey"/> class.
        /// </summary>
        public AttackCacheKey(Position position, Piece piece)
        {
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException("Cannot be empty square.", "piece");
            }

            #endregion

            _position = position;
            _piece = piece;
        }

        #endregion

        #region Public Properties

        public Position Position
        {
            [DebuggerStepThrough]
            get
            {
                return _position;
            }
        }

        public Piece Piece
        {
            [DebuggerStepThrough]
            get
            {
                return _piece;
            }
        }

        #endregion
    }
}