using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.Internal;
using Omnifactotum.Annotations;

//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization
//// ReSharper disable ReturnTypeCanBeEnumerable.Local - Using simpler types (such as arrays) for speed optimization
//// ReSharper disable SuggestBaseTypeForParameter - Using specific types (such as arrays) for speed optimization

namespace ChessPlatform
{
    public sealed class PiecePosition
    {
        private static readonly int PieceArrayLength = ChessConstants.Pieces.Max(item => (int)item) + 1;
        private static readonly int GameSideArrayLength = ChessConstants.GameSides.Max(item => (int)item) + 1;

        private readonly Piece[] _pieces;
        private readonly Bitboard[] _pieceBitboards;
        private readonly Bitboard[] _sideBitboards;

        public PiecePosition()
        {
            _pieces = new Piece[ChessConstants.SquareCount];

            _pieceBitboards = new Bitboard[PieceArrayLength];
            _pieceBitboards[GetPieceArrayIndex(Piece.None)] = Bitboard.Everything;

            _sideBitboards = new Bitboard[GameSideArrayLength];
            ZobristKey = ComputeZobristKey(_pieces);
        }

        private PiecePosition([NotNull] PiecePosition other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _pieces = other._pieces.Copy();
            _pieceBitboards = other._pieceBitboards.Copy();
            _sideBitboards = other._sideBitboards.Copy();
            ZobristKey = other.ZobristKey;
        }

        public long ZobristKey
        {
            get;
            private set;
        }

        public Piece this[Square square]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pieces[square.SquareIndex];
        }

        public Bitboard this[Piece piece]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pieceBitboards[GetPieceArrayIndex(piece)];
        }

        public Bitboard this[GameSide side]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sideBitboards[GetGameSideArrayIndex(side)];
        }

        public static bool TryCreate([NotNull] string piecePositionFen, out PiecePosition result)
        {
            result = new PiecePosition();
            if (result.TrySetupByFenSnippet(piecePositionFen))
            {
                return true;
            }

            result = null;
            return false;
        }

        public override string ToString() => this.GetFenSnippet();

        public void EnsureConsistency()
        {
            if (!DebugConstants.EnsurePieceDataConsistency)
            {
                return;
            }

            EnsureConsistencyInternal();
        }

        public PiecePosition Copy() => new PiecePosition(this);

        public bool IsSamePosition([NotNull] PiecePosition other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var length = _pieceBitboards.Length;
            if (length != other._pieceBitboards.Length)
            {
                return false;
            }

            for (var index = 0; index < length; index++)
            {
                if (_pieceBitboards[index] != other._pieceBitboards[index])
                {
                    return false;
                }
            }

            return true;
        }

        public Piece SetPiece(Square square, Piece piece)
        {
            var squareIndex = square.SquareIndex;
            var bitboardBit = square.Bitboard;

            var oldPiece = _pieces[squareIndex];
            _pieces[squareIndex] = piece;

            _pieceBitboards[GetPieceArrayIndex(oldPiece)] &= ~bitboardBit;

            var oldPieceSide = oldPiece.GetSide();
            if (oldPieceSide.HasValue)
            {
                _sideBitboards[GetGameSideArrayIndex(oldPieceSide.Value)] &= ~bitboardBit;
            }

            _pieceBitboards[GetPieceArrayIndex(piece)] |= bitboardBit;

            var pieceSide = piece.GetSide();
            if (pieceSide.HasValue)
            {
                _sideBitboards[GetGameSideArrayIndex(pieceSide.Value)] |= bitboardBit;
            }

            ZobristKey ^= ZobristHashHelper.GetPieceHash(square, oldPiece)
                ^ ZobristHashHelper.GetPieceHash(square, piece);

            return oldPiece;
        }

        public void SetupNewPiece(Square square, Piece piece)
        {
            if (piece == Piece.None)
            {
                throw new ArgumentException($@"Must be a specific piece rather than {Piece.None}.", nameof(piece));
            }

            var existingPiece = this[square];
            if (existingPiece != Piece.None)
            {
                throw new ChessPlatformException(
                    $@"The board square '{square}' is already occupied by '{existingPiece}'.");
            }

            SetPiece(square, piece);
        }

        internal void SetupByFenSnippet(string piecePositionFen)
        {
            const string InvalidFenMessage = "Invalid piece position FEN.";

            if (!TrySetupByFenSnippet(piecePositionFen))
            {
                throw new ArgumentException(InvalidFenMessage, nameof(piecePositionFen));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPieceArrayIndex(Piece piece) => (int)piece;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetGameSideArrayIndex(GameSide side) => (int)side;

        private static long ComputeZobristKey([NotNull] Piece[] pieces)
        {
            var result = ChessHelper.AllSquares.Aggregate(
                0L,
                (accumulator, square) =>
                    accumulator ^ ZobristHashHelper.GetPieceHash(square, pieces[square.SquareIndex]));

            return result;
        }

        private void EnsureConsistencyInternal()
        {
            foreach (var square in ChessHelper.AllSquares)
            {
                var piece = _pieces[square.SquareIndex];

                foreach (var currentPiece in ChessConstants.Pieces)
                {
                    var isSamePiece = piece == currentPiece;
                    var isSet = (this[currentPiece] & square.Bitboard).IsAny;
                    if (isSamePiece != isSet)
                    {
                        throw new ChessPlatformException(
                            $@"Bitboard inconsistency for the piece '{piece.GetName()}' at '{square}'.");
                    }
                }
            }

            var allBitboards = ChessConstants.Pieces.Select(piece => this[piece]).ToArray();
            for (var outerIndex = 0; outerIndex < allBitboards.Length; outerIndex++)
            {
                var outerBitboard = allBitboards[outerIndex];
                for (var innerIndex = outerIndex + 1; innerIndex < allBitboards.Length; innerIndex++)
                {
                    var innerBitboard = allBitboards[innerIndex];
                    var intersectionBitboard = outerBitboard & innerBitboard;
                    if (intersectionBitboard.IsNone)
                    {
                        continue;
                    }

                    var intersectingSquaresString =
                        intersectionBitboard.GetSquares().Select(item => item.ToString()).Join("', '");

                    throw new ChessPlatformException($@"Bitboard inconsistency at '{intersectingSquaresString}'.");
                }
            }

            foreach (var side in ChessConstants.GameSides)
            {
                var actual = this[side];
                var expected = GetEntireSideBitboardNonCached(side);
                if (actual != expected)
                {
                    throw new ChessPlatformException(
                        $@"Entire-side-bitboard inconsistency: expected '{expected}', actual '{actual}'.");
                }
            }
        }

        private Bitboard GetEntireSideBitboardNonCached(GameSide side)
            => ChessConstants.GameSideToPiecesMap[side].Aggregate(
                Bitboard.None,
                (accumulator, piece) => accumulator | this[piece]);

        private bool TrySetupByFenSnippet([NotNull] string piecePositionFen)
        {
            if (piecePositionFen.IsNullOrWhiteSpace())
            {
                return false;
            }

            var currentRank = ChessConstants.RankCount - 1;
            var currentFile = 0;
            foreach (var ch in piecePositionFen)
            {
                if (ch == ChessConstants.FenRankSeparator)
                {
                    if (currentFile != ChessConstants.FileCount)
                    {
                        return false;
                    }

                    currentFile = 0;
                    currentRank--;

                    if (currentRank < 0)
                    {
                        return false;
                    }

                    continue;
                }

                if (currentFile >= ChessConstants.FileCount)
                {
                    return false;
                }

                if (ChessConstants.FenCharToPieceMap.TryGetValue(ch, out var piece))
                {
                    var square = new Square(currentFile, currentRank);
                    SetupNewPiece(square, piece);
                    currentFile++;
                    continue;
                }

                var emptySquareCount = byte.Parse(new string(ch, 1));
                if (emptySquareCount == 0)
                {
                    return false;
                }

                currentFile += emptySquareCount;
            }

            return currentFile == ChessConstants.FileCount;
        }
    }
}