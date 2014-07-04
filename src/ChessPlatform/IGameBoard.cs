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
        ReadOnlyDictionary<GameMove, GameMoveInfo> ValidMoves
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

        [CanBeNull]
        IGameBoard PreviousBoard
        {
            get;
        }

        [CanBeNull]
        GameMove PreviousMove
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
        Position[] GetPositions(Piece piece);

        [NotNull]
        Position[] GetPositions(PieceColor color);

        bool IsValidMove([NotNull] GameMove move);

        bool IsPawnPromotionMove([NotNull] GameMove move);

        bool IsCapturingMove([NotNull] GameMove move);

        [CanBeNull]
        CastlingInfo CheckCastlingMove([NotNull] GameMove move);

        Position[] GetAttacks(Position targetPosition, PieceColor attackingColor);

        [NotNull]
        GameMove[] GetValidMovesBySource(Position sourcePosition);

        [NotNull]
        GameMove[] GetValidMovesByDestination(Position destinationPosition);

        AutoDrawType GetAutoDrawType();

        [NotNull]
        PackedGameBoard Pack();

        [NotNull]
        IGameBoard MakeMove([NotNull] GameMove move);

        [NotNull]
        IGameBoard MakeNullMove();

        [NotNull]
        IGameBoard[] GetHistory();

        #endregion
    }
}