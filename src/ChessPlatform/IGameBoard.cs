using System.Diagnostics;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public interface IGameBoard
    {
        #region Properties

        PieceColor ActiveColor
        {
            [DebuggerStepThrough]
            get;
        }

        GameState State
        {
            [DebuggerStepThrough]
            get;
        }

        CastlingOptions CastlingOptions
        {
            [DebuggerStepThrough]
            get;
        }

        [CanBeNull]
        EnPassantCaptureInfo EnPassantCaptureInfo
        {
            [DebuggerStepThrough]
            get;
        }

        [NotNull]
        ReadOnlySet<PieceMove> ValidMoves
        {
            [DebuggerStepThrough]
            get;
        }

        int FullMoveCountBy50MoveRule
        {
            [DebuggerStepThrough]
            get;
        }

        int FullMoveIndex
        {
            [DebuggerStepThrough]
            get;
        }

        [CanBeNull]
        PieceMove PreviousMove
        {
            [DebuggerStepThrough]
            get;
        }

        Piece LastCapturedPiece
        {
            [DebuggerStepThrough]
            get;
        }

        Piece this[Position position]
        {
            get;
        }

        #endregion

        #region Methods

        [NotNull]
        string GetFen();

        PieceInfo GetPieceInfo(Position position);

        [NotNull]
        Position[] GetPiecePositions(Piece piece);

        bool IsValidMove([NotNull] PieceMove move);

        bool IsPawnPromotion([NotNull] PieceMove move);

        [CanBeNull]
        CastlingInfo CheckCastlingMove([NotNull] PieceMove move);

        Position[] GetAttacks(Position targetPosition, PieceColor attackingColor);

        [NotNull]
        PieceMove[] GetValidMovesBySource(Position sourcePosition);

        [NotNull]
        PieceMove[] GetValidMovesByDestination(Position destinationPosition);

        AutoDrawType GetAutoDrawType();

        [NotNull]
        PackedGameBoard Serialize();

        [NotNull]
        IGameBoard MakeMove([NotNull] PieceMove move);

        #endregion
    }
}