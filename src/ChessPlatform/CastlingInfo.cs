using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

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
            [NotNull] GameMove kingMove,
            [NotNull] GameMove rookMove,
            [NotNull] params Square[] emptySquares)
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

            if (kingMove.From.Rank != kingMove.To.Rank
                || Math.Abs(kingMove.From.SquareIndex - kingMove.To.SquareIndex) != 2)
            {
                throw new ArgumentException(
                    $@"Invalid castling move '{kingMove.ToUciNotation()}'.",
                    nameof(kingMove));
            }

            #endregion

            CastlingType = castlingType;
            CastlingSide = castlingType.GetSide();
            Option = castlingType.ToOption();
            KingMove = kingMove;
            RookMove = rookMove;
            EmptySquares = emptySquares.AsReadOnly();
            PassedSquare = new Square((kingMove.From.SquareIndex + kingMove.To.SquareIndex) / 2);
            GameSide = Option.IsAnySet(CastlingOptions.WhiteMask) ? GameSide.White : GameSide.Black;
        }

        #endregion

        #region Public Properties

        public CastlingType CastlingType
        {
            [DebuggerStepThrough]
            get;
        }

        public CastlingSide CastlingSide
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

        public ReadOnlyCollection<Square> EmptySquares
        {
            [DebuggerStepThrough]
            get;
        }

        public Square PassedSquare
        {
            [DebuggerStepThrough]
            get;
        }

        public GameSide GameSide
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"[{GetType().GetQualifiedName()}] {GameSide} {CastlingSide}: {KingMove}";
        }

        #endregion
    }
}