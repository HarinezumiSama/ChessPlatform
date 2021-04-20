using System;
using System.Collections.Generic;
using System.Linq;
using ChessPlatform.Internal;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class StandardGamePosition : GamePosition
    {
        private StandardGamePosition(
            [NotNull] PiecePosition piecePosition,
            GameSide activeSide,
            int fullMoveIndex,
            CastlingOptions castlingOptions,
            EnPassantCaptureInfo2? enPassantCaptureInfo,
            int halfMoveCountBy50MoveRule)
            : base(piecePosition, activeSide, fullMoveIndex)
        {
            if (piecePosition == null)
            {
                throw new ArgumentNullException(nameof(piecePosition));
            }

            if (halfMoveCountBy50MoveRule < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(halfMoveCountBy50MoveRule),
                    halfMoveCountBy50MoveRule,
                    @"The value cannot be negative.");
            }

            CastlingOptions = castlingOptions;
            EnPassantCaptureInfo = enPassantCaptureInfo;
            HalfMoveCountBy50MoveRule = halfMoveCountBy50MoveRule;

            //// TODO [HarinezumiSama] IMPORTANT: This Zobrist key algorithm is different from the one used in the Polyglot opening book (in the en-passant part)
            ZobristKey = PiecePosition.ZobristKey
                ^ ZobristHashHelper.GetCastlingHash(CastlingOptions)
                ^ ZobristHashHelper.GetEnPassantHash(
                    EnPassantCaptureInfo,
                    PiecePosition[ActiveSide.ToPiece(PieceType.Pawn)])
                ^ ZobristHashHelper.GetTurnHash(ActiveSide);
        }

        private StandardGamePosition([NotNull] StandardGamePosition other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            CastlingOptions = other.CastlingOptions;
            EnPassantCaptureInfo = other.EnPassantCaptureInfo;
            HalfMoveCountBy50MoveRule = other.HalfMoveCountBy50MoveRule;
            ZobristKey = other.ZobristKey;
        }

        public override long ZobristKey
        {
            get;
        }

        public CastlingOptions CastlingOptions
        {
            get;
        }

        public EnPassantCaptureInfo2? EnPassantCaptureInfo
        {
            get;
        }

        public int HalfMoveCountBy50MoveRule
        {
            get;
        }

        public int FullMoveCountBy50MoveRule => HalfMoveCountBy50MoveRule / 2;

        public static bool TryCreate(
            [NotNull] string fen,
            out string errorMessage,
            out StandardGamePosition result)
        {
            string errorDetails;
            if (TryCreateInternal(fen, out errorDetails, out result))
            {
                errorMessage = null;
                return true;
            }

            errorMessage = $@"Invalid FEN for standard chess '{fen}':{Environment.NewLine}{errorDetails}";
            return false;
        }

        public static bool TryCreate([NotNull] string fen, out StandardGamePosition gamePosition)
        {
            string errorDetails;
            return TryCreateInternal(fen, out errorDetails, out gamePosition);
        }

        public static StandardGamePosition Create([NotNull] string fen)
        {
            string errorMessage;
            StandardGamePosition gamePosition;
            if (!TryCreate(fen, out errorMessage, out gamePosition))
            {
                throw new ArgumentException(errorMessage, nameof(fen));
            }

            return gamePosition.EnsureNotNull();
        }

        public static StandardGamePosition CreateInitial() => Create(ChessConstants.DefaultInitialFen);

        public override GamePosition Copy() => new StandardGamePosition(this);

        public override bool IsSamePosition(GamePosition other) => IsSamePosition(other as StandardGamePosition);

        public override GamePosition MakeMove(GameMove2 move)
        {
            throw new NotImplementedException();
        }

        public bool IsSamePosition(StandardGamePosition other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return ZobristKey == other.ZobristKey
                && CastlingOptions == other.CastlingOptions
                && ActiveSide == other.ActiveSide
                && EnPassantCaptureInfo == other.EnPassantCaptureInfo
                && PiecePosition.IsSamePosition(other.PiecePosition);
        }

        private static bool TryCreateInternal(
            [NotNull] string fen,
            out string errorDetails,
            out StandardGamePosition result)
        {
            if (fen.IsNullOrWhiteSpace())
            {
                errorDetails = "The FEN cannot be empty.";
                result = null;
                return false;
            }

            GameSide activeSide;
            int halfMovesBy50MoveRule;
            int fullMoveIndex;

            var fenSnippets = fen
                .Trim()
                .Split(ChessConstants.FenSnippetSeparator.AsArray(), StringSplitOptions.None);
            if (fenSnippets.Length != ChessConstants.FenSnippetCount)
            {
                errorDetails = "Invalid FEN format.";
                result = null;
                return false;
            }

            var piecePositionFen = fenSnippets[0];
            PiecePosition piecePosition;
            if (!PiecePosition.TryCreate(piecePositionFen, out piecePosition))
            {
                errorDetails = "Invalid position of pieces.";
                result = null;
                return false;
            }

            var activeSideSnippet = fenSnippets[1];
            if (!ChessConstants.FenSnippetToGameSideMap.TryGetValue(activeSideSnippet, out activeSide))
            {
                errorDetails = "Invalid active side.";
                result = null;
                return false;
            }

            var castlingOptions = CastlingOptions.None;
            var castlingOptionsSnippet = fenSnippets[2];
            if (castlingOptionsSnippet != ChessConstants.NoneCastlingOptionsFenSnippet)
            {
                var castlingOptionsSnippetSet = OmnifactotumCollectionExtensions.ToHashSet(castlingOptionsSnippet);
                foreach (var optionChar in castlingOptionsSnippetSet)
                {
                    CastlingOptions option;
                    if (!ChessConstants.FenCharCastlingOptionMap.TryGetValue(optionChar, out option))
                    {
                        errorDetails = "Invalid castling options.";
                        result = null;
                        return false;
                    }

                    castlingOptions |= option;
                }
            }

            EnPassantCaptureInfo2? enPassantCaptureInfo = null;
            var enPassantCaptureTargetSnippet = fenSnippets[3];
            if (enPassantCaptureTargetSnippet != ChessConstants.NoEnPassantCaptureFenSnippet)
            {
                const string InvalidEnPassant = "Invalid en-passant.";

                var captureSquare = Square.TryFromAlgebraic(enPassantCaptureTargetSnippet);
                if (!captureSquare.HasValue)
                {
                    errorDetails = InvalidEnPassant;
                    result = null;
                    return false;
                }

                var enPassantInfo =
                    ChessConstants.GameSideToDoublePushInfoMap.Values.SingleOrDefault(
                        obj => obj.CaptureTargetRank == captureSquare.Value.Rank);

                if (enPassantInfo == null)
                {
                    errorDetails = InvalidEnPassant;
                    result = null;
                    return false;
                }

                enPassantCaptureInfo = new EnPassantCaptureInfo2(
                    captureSquare.Value,
                    new Square(captureSquare.Value.File, enPassantInfo.EndRank));
            }

            var halfMovesBy50MoveRuleSnippet = fenSnippets[4];
            if (!ChessHelper.TryParseInt(halfMovesBy50MoveRuleSnippet, out halfMovesBy50MoveRule)
                || halfMovesBy50MoveRule < 0)
            {
                errorDetails = "Invalid half move counter for the 50 move rule.";
                result = null;
                return false;
            }

            var fullMoveIndexSnippet = fenSnippets[5];
            if (!ChessHelper.TryParseInt(fullMoveIndexSnippet, out fullMoveIndex) || fullMoveIndex <= 0)
            {
                errorDetails = "Invalid move index.";
                result = null;
                return false;
            }

            result = new StandardGamePosition(
                piecePosition,
                activeSide,
                fullMoveIndex,
                castlingOptions,
                enPassantCaptureInfo,
                halfMovesBy50MoveRule);

            errorDetails = null;
            return true;
        }
    }
}