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
            get;
        }

        GameState State
        {
            get;
        }

        CastlingOptions CastlingOptions
        {
            get;
        }

        [CanBeNull]
        EnPassantCaptureInfo EnPassantCaptureInfo
        {
            get;
        }

        [NotNull]
        ReadOnlyDictionary<PieceMove, PieceMoveInfo> ValidMoves
        {
            get;
        }

        int FullMoveCountBy50MoveRule
        {
            get;
        }

        int FullMoveIndex
        {
            get;
        }

        IGameBoard PreviousBoard
        {
            get;
        }

        [CanBeNull]
        PieceMove PreviousMove
        {
            get;
        }

        Piece LastCapturedPiece
        {
            get;
        }

        bool CanMakeNullMove
        {
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

        [NotNull]
        Position[] GetPiecePositions(PieceColor color);

        bool IsValidMove([NotNull] PieceMove move);

        bool IsPawnPromotionMove([NotNull] PieceMove move);

        bool IsCapturingMove([NotNull] PieceMove move);

        [CanBeNull]
        CastlingInfo CheckCastlingMove([NotNull] PieceMove move);

        Position[] GetAttacks(Position targetPosition, PieceColor attackingColor);

        [NotNull]
        PieceMove[] GetValidMovesBySource(Position sourcePosition);

        [NotNull]
        PieceMove[] GetValidMovesByDestination(Position destinationPosition);

        AutoDrawType GetAutoDrawType();

        [NotNull]
        PackedGameBoard Pack();

        [NotNull]
        IGameBoard MakeMove([NotNull] PieceMove move);

        [NotNull]
        IGameBoard MakeNullMove();

        [NotNull]
        IGameBoard[] GetHistory();

        #endregion
    }
}