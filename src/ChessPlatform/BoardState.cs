using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class BoardState
    {
        #region Constants and Fields

        private readonly Piece[] _pieces;
        private readonly Dictionary<Piece, HashSet<byte>> _pieceOffsetMap;

        private readonly PieceColor _activeColor;
        private readonly PieceColor? _colorInCheck;
        private readonly bool _isStalemate;
        private readonly PieceColor? _checkmatingColor;
        private readonly CastlingOptions _castlingOptions;
        private readonly Position? _enPassantTarget;
        private readonly ReadOnlySet<PieceMove> _validMoves;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState()
        {
            CreatePieces(out _pieces, out _pieceOffsetMap);
            SetupDefault(out _activeColor, out _castlingOptions, out _enPassantTarget);
            PostInitialize(out _colorInCheck, out _validMoves, out _isStalemate, out _checkmatingColor);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState([NotNull] string fen)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "fen");
            }

            #endregion

            CreatePieces(out _pieces, out _pieceOffsetMap);

            //// TODO [vmcl] Initialize board with the specified FEN

            //// _activeColor = ...
            //// _castlingOptions = ...
            ////_enPassantTarget = ...

            PostInitialize(out _colorInCheck, out _validMoves, out _isStalemate, out _checkmatingColor);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        private BoardState(
            [NotNull] BoardState previousState,
            [NotNull] PieceMove move,
            [CanBeNull] PieceType? promotedPieceType)
        {
            #region Argument Check

            if (previousState == null)
            {
                throw new ArgumentNullException("previousState");
            }

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            _pieces = previousState._pieces.Copy();
            _pieceOffsetMap = CopyPieceOffsetMap(previousState._pieceOffsetMap);
            _activeColor = previousState._activeColor.Invert();
            _castlingOptions = previousState.CastlingOptions;

            var movingPiece = SetPiece(move.From, Piece.None);

            movingPiece.EnsureDefined();

            var color = movingPiece.GetColor();
            if (movingPiece == Piece.None || !color.HasValue)
            {
                throw new ArgumentException("The move starting position contains no piece.", "move");
            }

            var removed = _pieceOffsetMap.GetValueOrCreate(movingPiece).Remove(move.From.X88Value);
            if (!removed)
            {
                throw new InvalidOperationException("Inconsistent state of the piece offset map.");
            }

            var pieceColor = color.Value;

            if (pieceColor != previousState._activeColor)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The move starting position specifies piece of invalid color (expected: {0}).",
                        previousState._activeColor),
                    "move");
            }

            var pieceType = movingPiece.GetPieceType();
            if (pieceType == PieceType.Pawn
                && (pieceColor == PieceColor.White && move.To.Rank == ChessConstants.WhitePawnPromotionRank
                    || pieceColor == PieceColor.Black && move.To.Rank == ChessConstants.BlackPawnPromotionRank))
            {
                if (!promotedPieceType.HasValue)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The promoted piece type is not specified for promoted {0}.",
                            movingPiece.GetDescription()),
                        "promotedPieceType");
                }

                movingPiece = promotedPieceType.Value.ToPiece(pieceColor);
            }

            var capturedPiece = SetPiece(move.To, movingPiece);
            if (capturedPiece != Piece.None)
            {
                if (capturedPiece.GetColor() == pieceColor)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move destination position performs a capture of the same color ({0}).",
                            pieceColor),
                        "move");
                }

                var capturedRemoved = _pieceOffsetMap.GetValueOrCreate(capturedPiece).Remove(move.To.X88Value);
                if (!capturedRemoved)
                {
                    throw new InvalidOperationException("Inconsistent state of the piece offset map.");
                }
            }

            var added = _pieceOffsetMap.GetValueOrCreate(movingPiece).Add(move.To.X88Value);
            if (!added)
            {
                throw new InvalidOperationException("Inconsistent state of the piece offset map.");
            }

            if (CastlingOptions != CastlingOptions.None)
            {
                //// TODO [vmcl] Adjust castling options
            }

            //// TODO [vmcl] Set en passant target, if applicable
            //enPassantTarget=...

            PostInitialize(out _colorInCheck, out _validMoves, out _isStalemate, out _checkmatingColor);
            Validate();
        }

        #endregion

        #region Public Properties

        public PieceColor ActiveColor
        {
            [DebuggerStepThrough]
            get
            {
                return _activeColor;
            }
        }

        public PieceColor? ColorInCheck
        {
            [DebuggerStepThrough]
            get
            {
                return _colorInCheck;
            }
        }

        public bool IsStalemate
        {
            [DebuggerStepThrough]
            get
            {
                return _isStalemate;
            }
        }

        public PieceColor? CheckmatingColor
        {
            [DebuggerStepThrough]
            get
            {
                return _checkmatingColor;
            }
        }

        public CastlingOptions CastlingOptions
        {
            [DebuggerStepThrough]
            get
            {
                return _castlingOptions;
            }
        }

        public Position? EnPassantTarget
        {
            [DebuggerStepThrough]
            get
            {
                return _enPassantTarget;
            }
        }

        public ReadOnlySet<PieceMove> ValidMoves
        {
            [DebuggerStepThrough]
            get
            {
                return _validMoves;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return GetFen();
        }

        public string GetFen()
        {
            var resultBuilder = new StringBuilder((ChessConstants.FileCount + 1) * ChessConstants.RankCount + 20);

            var emptySquareCount = new ValueContainer<int>(0);
            Action writeEmptyCount =
                () =>
                {
                    if (emptySquareCount.Value > 0)
                    {
                        resultBuilder.Append(emptySquareCount.Value);
                        emptySquareCount.Value = 0;
                    }
                };

            for (var rank = ChessConstants.RankCount - 1; rank >= 0; rank--)
            {
                if (rank < ChessConstants.RankCount - 1)
                {
                    resultBuilder.Append('/');
                }

                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    var piece = GetPiece(new Position((byte)file, (byte)rank));
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    writeEmptyCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                writeEmptyCount();
            }

            //// TODO [vmcl] Consider actual: (a) castling availability; (b) en passant target; (c) half move clock; (d) full move number
            resultBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                " {0} {1} - 0 1",
                _activeColor == PieceColor.White ? 'w' : 'b',
                _castlingOptions.GetFenSnippet());

            return resultBuilder.ToString();
        }

        public BoardState MakeMove([NotNull] PieceMove move, [CanBeNull] PieceType? promotedPieceType)
        {
            return new BoardState(this, move, promotedPieceType);
        }

        #endregion

        #region Private Methods

        private static void CreatePieces(out Piece[] pieces, out Dictionary<Piece, HashSet<byte>> pieceOffsetMap)
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            pieces = new Piece[ChessConstants.X88Length];
            pieceOffsetMap = new Dictionary<Piece, HashSet<byte>>();
        }

        private static Dictionary<Piece, HashSet<byte>> CopyPieceOffsetMap(
            ICollection<KeyValuePair<Piece, HashSet<byte>>> pieceOffsetMap)
        {
            var result = new Dictionary<Piece, HashSet<byte>>(pieceOffsetMap.Count);
            foreach (var pair in pieceOffsetMap)
            {
                result.Add(pair.Key, new HashSet<byte>(pair.Value));
            }

            return result;
        }

        private void Validate()
        {
            //// TODO [vmcl] (1) Count kings, (2) no kings near each other, (3) no 2 kings under check, (4) no more than 16 pieces of each color, (5) etc.
        }

        private void PostInitialize(
            out PieceColor? colorInCheck,
            out ReadOnlySet<PieceMove> validMoves,
            out bool isStalemate,
            out PieceColor? checkmatingColor)
        {
            var activePairs = _pieceOffsetMap
                .Where(pair => pair.Key.GetColor() == _activeColor && pair.Value.Count != 0)
                .ToArray();

            foreach (var pair in activePairs)
            {
                ;
            }

            GetPotentialMoves("a1").ToString();

            throw new NotImplementedException();
        }

        private void SetupDefault(
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out Position? enPassantTarget)
        {
            SetupNewPiece(PieceType.Rook, PieceColor.White, "a1");
            SetupNewPiece(PieceType.Knight, PieceColor.White, "b1");
            SetupNewPiece(PieceType.Bishop, PieceColor.White, "c1");
            SetupNewPiece(PieceType.Queen, PieceColor.White, "d1");
            SetupNewPiece(PieceType.King, PieceColor.White, "e1");
            SetupNewPiece(PieceType.Bishop, PieceColor.White, "f1");
            SetupNewPiece(PieceType.Knight, PieceColor.White, "g1");
            SetupNewPiece(PieceType.Rook, PieceColor.White, "h1");
            Position.GenerateRank(1).DoForEach(position => SetupNewPiece(PieceType.Pawn, PieceColor.White, position));

            Position.GenerateRank(6).DoForEach(position => SetupNewPiece(PieceType.Pawn, PieceColor.Black, position));
            SetupNewPiece(PieceType.Rook, PieceColor.Black, "a8");
            SetupNewPiece(PieceType.Knight, PieceColor.Black, "b8");
            SetupNewPiece(PieceType.Bishop, PieceColor.Black, "c8");
            SetupNewPiece(PieceType.Queen, PieceColor.Black, "d8");
            SetupNewPiece(PieceType.King, PieceColor.Black, "e8");
            SetupNewPiece(PieceType.Bishop, PieceColor.Black, "f8");
            SetupNewPiece(PieceType.Knight, PieceColor.Black, "g8");
            SetupNewPiece(PieceType.Rook, PieceColor.Black, "h8");

            activeColor = PieceColor.White;
            castlingOptions = CastlingOptions.All;
            enPassantTarget = null;
        }

        private void SetupNewPiece(PieceType pieceType, PieceColor color, Position position)
        {
            var offset = position.X88Value;
            var existingPiece = _pieces[offset];
            if (existingPiece != Piece.None)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            var piece = pieceType.ToPiece(color);
            _pieces[offset] = piece;

            var added = _pieceOffsetMap.GetValueOrCreate(piece).Add(offset);
            if (!added)
            {
                throw new InvalidOperationException("Inconsistent state of the piece offset map.");
            }
        }

        private Piece SetPiece(Position position, Piece piece)
        {
            var offset = position.X88Value;
            var oldPiece = _pieces[offset];
            _pieces[offset] = piece;

            return oldPiece;
        }

        private Piece GetPiece(Position position)
        {
            var offset = position.X88Value;
            return _pieces[offset];
        }

        private PieceMove[] GetPotentialMoves(Position position)
        {
            var piece = GetPiece(position);
            var pieceType = piece.GetPieceType();
            var color = piece.GetColor();

            Position[] positions;
            switch (pieceType)
            {
                case PieceType.King:
                    var kingOffsets = new byte[]
                    {
                        0xFF,
                        0x01,
                        0xF0,
                        0x10,
                        0x1F,
                        0xF1,
                        0x11,
                        0xEF,
                        (byte)(CastlingOptions.IsAnySet(CastlingOptions.WhiteKingSide) ? 0x02 : 0),
                        (byte)(CastlingOptions.IsAnySet(CastlingOptions.WhiteQueenSide) ? 0xFE : 0)
                    };

                    positions = position.GetValidPositions(kingOffsets);
                    break;

                default:
                    throw pieceType.CreateEnumValueNotImplementedException();
            }

            var result = positions.Select(item => new PieceMove(position, item)).ToArray();
            return result;
        }

        #endregion
    }
}