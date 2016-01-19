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
        #region Constants and Fields

        private static readonly int PieceArrayLength = ChessConstants.Pieces.Max(item => (int)item) + 1;
        private static readonly int GameSideArrayLength = ChessConstants.GameSides.Max(item => (int)item) + 1;

        private readonly Piece[] _pieces;
        private readonly Bitboard[] _pieceBitboards;
        private readonly Bitboard[] _sideBitboards;

        #endregion

        #region Constructors

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
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            #endregion

            _pieces = other._pieces.Copy();
            _pieceBitboards = other._pieceBitboards.Copy();
            _sideBitboards = other._sideBitboards.Copy();
            ZobristKey = other.ZobristKey;
        }

        #endregion

        #region Public Properties

        public long ZobristKey
        {
            get;
            private set;
        }

        public Piece this[Square square]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _pieces[square.SquareIndex];
            }
        }

        public Bitboard this[Piece piece]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _pieceBitboards[GetPieceArrayIndex(piece)];
            }
        }

        public Bitboard this[GameSide side]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _sideBitboards[GetGameSideArrayIndex(side)];
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.GetFenSnippet();
        }

        public void EnsureConsistency()
        {
            if (!DebugConstants.EnsurePieceDataConsistency)
            {
                return;
            }

            EnsureConsistencyInternal();
        }

        public PiecePosition Copy()
        {
            return new PiecePosition(this);
        }

        public bool IsSamePosition([NotNull] PiecePosition other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            #endregion

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
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException($@"Must be a specific piece rather than {Piece.None}.", nameof(piece));
            }

            #endregion

            var existingPiece = this[square];
            if (existingPiece != Piece.None)
            {
                throw new ChessPlatformException(
                    $@"The board square '{square}' is already occupied by '{existingPiece}'.");
            }

            SetPiece(square, piece);
        }

        public void SetupByFenSnippet(string fenSnippet)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fenSnippet))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(fenSnippet));
            }

            #endregion

            const string InvalidFenMessage = "Invalid FEN.";

            var currentRank = ChessConstants.RankCount - 1;
            var currentFile = 0;
            foreach (var ch in fenSnippet)
            {
                if (ch == ChessConstants.FenRankSeparator)
                {
                    if (currentFile != ChessConstants.FileCount)
                    {
                        throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                    }

                    currentFile = 0;
                    currentRank--;

                    if (currentRank < 0)
                    {
                        throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                    }

                    continue;
                }

                if (currentFile >= ChessConstants.FileCount)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                }

                Piece piece;
                if (ChessConstants.FenCharToPieceMap.TryGetValue(ch, out piece))
                {
                    var square = new Square(currentFile, currentRank);
                    SetupNewPiece(square, piece);
                    currentFile++;
                    continue;
                }

                var emptySquareCount = byte.Parse(new string(ch, 1));
                if (emptySquareCount == 0)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                }

                currentFile += emptySquareCount;
            }

            if (currentFile != ChessConstants.FileCount)
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
            }
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPieceArrayIndex(Piece piece)
        {
            return (int)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetGameSideArrayIndex(GameSide side)
        {
            return (int)side;
        }

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
                    var isSet = (this[currentPiece] & square.Bitboard).IsAny;
                    if ((piece == currentPiece) != isSet)
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
        {
            return ChessConstants.GameSideToPiecesMap[side].Aggregate(
                Bitboard.None,
                (accumulator, piece) => accumulator | this[piece]);
        }

        #endregion
    }
}