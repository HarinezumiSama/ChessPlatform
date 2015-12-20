using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    public sealed class CastlingInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CastlingInfo"/> class
        ///     using the specified parameters.
        /// </summary>
        internal CastlingInfo(
            CastlingType castlingType,
            GameMove kingMove,
            GameMove rookMove,
            params Position[] emptySquares)
        {
            #region Argument Check

            castlingType.EnsureDefined();

            if (kingMove == null)
            {
                throw new ArgumentNullException(nameof(kingMove));
            }

            if (rookMove == null)
            {
                throw new ArgumentNullException(nameof(rookMove));
            }

            if (emptySquares == null)
            {
                throw new ArgumentNullException(nameof(emptySquares));
            }

            if (Math.Abs(kingMove.From.X88Value - kingMove.To.X88Value) != 2)
            {
                throw new ArgumentException("Invalid castling move.", nameof(kingMove));
            }

            #endregion

            CastlingType = castlingType;
            Side = castlingType.GetSide();
            Option = castlingType.ToOption();
            KingMove = kingMove;
            RookMove = rookMove;
            EmptySquares = emptySquares.AsReadOnly();
            PassedPosition = new Position((byte)((kingMove.From.X88Value + kingMove.To.X88Value) / 2));
            Color = Option.IsAnySet(CastlingOptions.WhiteMask) ? PieceColor.White : PieceColor.Black;
        }

        #endregion

        #region Public Properties

        public CastlingType CastlingType
        {
            [DebuggerStepThrough]
            get;
        }

        public CastlingSide Side
        {
            [DebuggerStepThrough]
            get;
        }

        public CastlingOptions Option
        {
            [DebuggerStepThrough]
            get;
        }

        public GameMove KingMove
        {
            [DebuggerStepThrough]
            get;
        }

        public GameMove RookMove
        {
            [DebuggerStepThrough]
            get;
        }

        public ReadOnlyCollection<Position> EmptySquares
        {
            [DebuggerStepThrough]
            get;
        }

        public Position PassedPosition
        {
            [DebuggerStepThrough]
            get;
        }

        public PieceColor Color
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"[{GetType().GetQualifiedName()}] {Color} {Side}: {KingMove}";
        }

        #endregion
    }
}