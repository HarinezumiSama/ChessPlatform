using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Omnifactotum;

namespace ChessPlatform
{
    internal static class ChessHelper
    {
        #region Constants and Fields

        public static readonly ReadOnlyCollection<RayInfo> StraightRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0xFF, true),
                    new RayInfo(0x01, true),
                    new RayInfo(0xF0, true),
                    new RayInfo(0x10, true)
                });

        public static readonly ReadOnlyCollection<RayInfo> DiagonalRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0x0F, false),
                    new RayInfo(0x11, false),
                    new RayInfo(0xEF, false),
                    new RayInfo(0xF1, false)
                });

        public static readonly ReadOnlyCollection<RayInfo> AllRays =
            new ReadOnlyCollection<RayInfo>(StraightRays.Concat(DiagonalRays).ToArray());

        public static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x10, true) },
                    { PieceColor.Black, new RayInfo(0xF0, true) }
                });

        public static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnEnPassantMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x20, true) },
                    { PieceColor.Black, new RayInfo(0xE0, true) }
                });

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnAttackRayMap =
            new ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
                new Dictionary<PieceColor, ReadOnlySet<RayInfo>>
                {
                    {
                        PieceColor.White,
                        new[] { new RayInfo(0x0F, false), new RayInfo(0x11, false) }.ToHashSet().AsReadOnly()
                    },
                    {
                        PieceColor.Black,
                        new[] { new RayInfo(0xEF, false), new RayInfo(0xF1, false) }.ToHashSet().AsReadOnly()
                    }
                });

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
            PawnAttackRayMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(item => item.Offset).ToHashSet().AsReadOnly()).AsReadOnly();

        public static readonly ReadOnlySet<RayInfo> KingAttackRays = AllRays.ToHashSet().AsReadOnly();

        public static readonly ReadOnlySet<byte> KingAttackOffsets =
            KingAttackRays.Select(item => item.Offset).ToHashSet().AsReadOnly();

        public static readonly ReadOnlySet<byte> KnightAttackOffsets =
            new byte[] { 0x21, 0x1F, 0xE1, 0xDF, 0x12, 0x0E, 0xEE, 0xF2 }.ToHashSet().AsReadOnly();

        private const int MaxSlidingPieceDistance = 8;
        private const int MaxPawnAttackOrMoveDistance = 1;
        private const int MaxKingMoveDistance = 1;

        private static readonly ReadOnlyDictionary<CastlingOptions, CastlingInfo> CastlingInfos =
            new ReadOnlyDictionary<CastlingOptions, CastlingInfo>(
                new Dictionary<CastlingOptions, CastlingInfo>
                {
                    { CastlingOptions.WhiteKingSide, ChessConstants.WhiteCastlingKingSide },
                    { CastlingOptions.WhiteQueenSide, ChessConstants.WhiteCastlingQueenSide },
                    { CastlingOptions.BlackKingSide, ChessConstants.BlackCastlingKingSide },
                    { CastlingOptions.BlackQueenSide, ChessConstants.BlackCastlingQueenSide }
                });

        private static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>
            ColorToCastlingOptionsMap =
                new ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>(
                    new Dictionary<PieceColor, ReadOnlySet<CastlingOptions>>
                    {
                        {
                            PieceColor.White,
                            new[] { CastlingOptions.WhiteKingSide, CastlingOptions.WhiteQueenSide }
                                .ToHashSet()
                                .AsReadOnly()
                        },
                        {
                            PieceColor.Black,
                            new[] { CastlingOptions.BlackKingSide, CastlingOptions.BlackQueenSide }
                                .ToHashSet()
                                .AsReadOnly()
                        }
                    });

        #endregion

        #region Public Methods

        public static Position[] GetKnightMovePositions(Position position)
        {
            return GetOnboardPositions(position, KnightAttackOffsets);
        }

        #endregion

        #region Internal Methods

        internal static void ValidatePieces(ICollection<Piece> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException("pieces");
            }

            if (pieces.Count != ChessConstants.X88Length)
            {
                throw new ArgumentException("Invalid count.", "pieces");
            }
        }

        internal static Dictionary<Piece, HashSet<byte>> CopyPieceOffsetMap(
            ICollection<KeyValuePair<Piece, HashSet<byte>>> pieceOffsetMap)
        {
            #region Argument Check

            if (pieceOffsetMap == null)
            {
                throw new ArgumentNullException("pieceOffsetMap");
            }

            #endregion

            var result = new Dictionary<Piece, HashSet<byte>>(pieceOffsetMap.Count);
            foreach (var pair in pieceOffsetMap)
            {
                result.Add(pair.Key, new HashSet<byte>(pair.Value.EnsureNotNull()));
            }

            return result;
        }

        internal static Piece GetPiece(IList<Piece> pieces, Position position)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            return pieces[position.X88Value];
        }

        internal static PieceInfo GetPieceInfo(IList<Piece> pieces, Position position)
        {
            var piece = GetPiece(pieces, position);
            return new PieceInfo(piece);
        }

        internal static Piece SetPiece(IList<Piece> pieces, Position position, Piece piece)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var offset = position.X88Value;
            var oldPiece = pieces[offset];
            pieces[offset] = piece;

            return oldPiece;
        }

        internal static void GetFenSnippet(IList<Piece> pieces, StringBuilder resultBuilder)
        {
            #region Argument Check

            ValidatePieces(pieces);

            if (resultBuilder == null)
            {
                throw new ArgumentNullException("resultBuilder");
            }

            #endregion

            var emptySquareCount = new ValueContainer<int>(0);
            Action writeEmptySquareCount =
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
                    var piece = GetPiece(pieces, new Position((byte)file, (byte)rank));
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    writeEmptySquareCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                writeEmptySquareCount();
            }
        }

        internal static string GetFenSnippet(IList<Piece> pieces)
        {
            var resultBuilder = new StringBuilder();
            GetFenSnippet(pieces, resultBuilder);
            return resultBuilder.ToString();
        }

        internal static Position[] GetAttacks(IList<Piece> pieces, Position targetPosition, PieceColor attackingColor)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var resultList = new List<Position>();

            var attackingKnights = GetKnightMovePositions(targetPosition)
                .Where(p => pieces[p.X88Value].GetColor() == attackingColor)
                .ToArray();

            resultList.AddRange(attackingKnights);

            foreach (var rayOffset in AllRays)
            {
                for (var currentX88Value = (byte)(targetPosition.X88Value + rayOffset.Offset);
                    Position.IsValidX88Value(currentX88Value);
                    currentX88Value += rayOffset.Offset)
                {
                    var currentPosition = new Position(currentX88Value);

                    var piece = pieces[currentPosition.X88Value];
                    var color = piece.GetColor();
                    if (piece == Piece.None || !color.HasValue)
                    {
                        continue;
                    }

                    if (color.Value != attackingColor)
                    {
                        break;
                    }

                    var pieceType = piece.GetPieceType();
                    if ((pieceType.IsSlidingStraight() && rayOffset.IsStraight)
                        || (pieceType.IsSlidingDiagonally() && !rayOffset.IsStraight))
                    {
                        resultList.Add(currentPosition);
                        break;
                    }

                    var difference = (byte)(targetPosition.X88Value - currentX88Value);
                    switch (pieceType)
                    {
                        case PieceType.Pawn:
                            if (PawnAttackOffsetMap[attackingColor].Contains(difference))
                            {
                                resultList.Add(currentPosition);
                            }

                            break;

                        case PieceType.King:
                            if (KingAttackOffsets.Contains(difference))
                            {
                                resultList.Add(currentPosition);
                            }

                            break;
                    }

                    break;
                }
            }

            return resultList.ToArray();
        }

        internal static bool IsUnderAttack(IList<Piece> pieces, Position targetPosition, PieceColor attackingColor)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var attacks = GetAttacks(pieces, targetPosition, attackingColor);
            return attacks.Length != 0;
        }

        internal static bool IsAnyUnderAttack(
            IList<Piece> pieces,
            IEnumerable<Position> targetPositions,
            PieceColor attackingColor)
        {
            #region Argument Check

            ValidatePieces(pieces);

            if (targetPositions == null)
            {
                throw new ArgumentNullException("targetPositions");
            }

            #endregion

            var result = targetPositions.Any(p => IsUnderAttack(pieces, p, attackingColor));
            return result;
        }

        internal static bool IsInCheck(
            IList<Piece> pieces,
            IDictionary<Piece, HashSet<byte>> pieceOffsetMap,
            PieceColor kingColor)
        {
            #region Argument Check

            ValidatePieces(pieces);

            if (pieceOffsetMap == null)
            {
                throw new ArgumentNullException("pieceOffsetMap");
            }

            kingColor.EnsureDefined();

            #endregion

            var king = PieceType.King.ToPiece(kingColor);
            var kingPosition = new Position(pieceOffsetMap[king].Single());

            return IsUnderAttack(pieces, kingPosition, kingColor.Invert());
        }

        internal static Position? GetEnPassantCaptureTarget(IList<Piece> pieces, PieceMove move)
        {
            #region Argument Check

            ValidatePieces(pieces);

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var pieceInfo = GetPieceInfo(pieces, move.From);
            if (!pieceInfo.Color.HasValue || pieceInfo.PieceType != PieceType.Pawn || move.From.File != move.To.File)
            {
                return null;
            }

            var enPassantInfo = ChessConstants.ColorToEnPassantInfoMap[pieceInfo.Color.Value].EnsureNotNull();
            var isEnPassant = move.From.Rank == enPassantInfo.StartRank && move.To.Rank == enPassantInfo.EndRank;

            return isEnPassant ? new Position(move.From.File, enPassantInfo.CaptureTargetRank) : null;
        }

        internal static Position[] GetPotentialMovePositions(
            IList<Piece> pieces,
            CastlingOptions castlingOptions,
            Position? enPassantCaptureTarget,
            Position sourcePosition)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var pieceInfo = GetPieceInfo(pieces, sourcePosition);
            if (pieceInfo.PieceType == PieceType.None || !pieceInfo.Color.HasValue)
            {
                throw new ArgumentException("No piece at the source position.", "sourcePosition");
            }

            var pieceColor = pieceInfo.Color.Value;

            if (pieceInfo.PieceType == PieceType.Knight)
            {
                var result = GetKnightMovePositions(sourcePosition)
                    .Where(position => GetPiece(pieces, position).GetColor() != pieceColor)
                    .ToArray();

                return result;
            }

            if (pieceInfo.PieceType == PieceType.King)
            {
                var result = GetKingPotentialMovePositions(pieces, castlingOptions, sourcePosition, pieceColor);
                return result;
            }

            if (pieceInfo.PieceType == PieceType.Pawn)
            {
                var result = GetPawnPotentialMovePositions(pieces, enPassantCaptureTarget, sourcePosition, pieceColor);
                return result;
            }

            var resultList = new List<Position>();

            if (pieceInfo.PieceType.IsSlidingStraight())
            {
                GetPotentialMovePositionsByRays(
                    pieces,
                    sourcePosition,
                    pieceColor,
                    StraightRays,
                    MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            if (pieceInfo.PieceType.IsSlidingDiagonally())
            {
                GetPotentialMovePositionsByRays(
                    pieces,
                    sourcePosition,
                    pieceColor,
                    DiagonalRays,
                    MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            return resultList.ToArray();
        }

        internal static CastlingInfo CheckCastlingMove(IList<Piece> pieces, PieceMove move)
        {
            var piece = GetPiece(pieces, move.From);
            var color = piece.GetColor();
            if (piece.GetPieceType() != PieceType.King || !color.HasValue)
            {
                return null;
            }

            var castlingOptions = ColorToCastlingOptionsMap[color.Value];

            var result = CastlingInfos
                .SingleOrDefault(pair => castlingOptions.Contains(pair.Key) && pair.Value.CastlingMove == move)
                .Value;

            return result;
        }

        internal static PieceMove GetEnPassantMove(IList<Piece> pieces, Position sourcePosition)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var pieceInfo = GetPieceInfo(pieces, sourcePosition);
            if (pieceInfo.PieceType != PieceType.Pawn || !pieceInfo.Color.HasValue)
            {
                return null;
            }

            var enPassantInfo = ChessConstants.ColorToEnPassantInfoMap[pieceInfo.Color.Value].EnsureNotNull();
            if (sourcePosition.Rank != enPassantInfo.StartRank)
            {
                return null;
            }

            var destinationPosition = new Position(sourcePosition.File, enPassantInfo.EndRank);
            var intermediatePosition = new Position(sourcePosition.File, enPassantInfo.CaptureTargetRank);
            var isEnPassant = CheckSquares(pieces, Piece.None, intermediatePosition, destinationPosition);

            return isEnPassant ? new PieceMove(sourcePosition, destinationPosition) : null;
        }

        #endregion

        #region Private Methods

        private static Position[] GetOnboardPositions(Position position, ICollection<byte> x88Offsets)
        {
            #region Argument Check

            if (x88Offsets == null)
            {
                throw new ArgumentNullException("x88Offsets");
            }

            #endregion

            var result = new List<Position>(x88Offsets.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var x88Offset in x88Offsets)
            {
                if (x88Offset == 0)
                {
                    continue;
                }

                var x88Value = (byte)(position.X88Value + x88Offset);
                if (Position.IsValidX88Value(x88Value))
                {
                    result.Add(new Position(x88Value));
                }
            }

            return result.ToArray();
        }

        private static void GetPotentialMovePositionsByRays(
            IList<Piece> pieces,
            Position sourcePosition,
            PieceColor sourceColor,
            IEnumerable<RayInfo> rays,
            int maxDistance,
            bool allowCapturing,
            ICollection<Position> resultCollection)
        {
            foreach (var ray in rays)
            {
                for (byte currentX88Value = (byte)(sourcePosition.X88Value + ray.Offset), distance = 1;
                    Position.IsValidX88Value(currentX88Value) && distance <= maxDistance;
                    currentX88Value += ray.Offset, distance++)
                {
                    var currentPosition = new Position(currentX88Value);

                    var currentPiece = pieces[currentPosition.X88Value];
                    var currentColor = currentPiece.GetColor();
                    if (currentPiece == Piece.None || !currentColor.HasValue)
                    {
                        resultCollection.Add(currentPosition);
                        continue;
                    }

                    if (currentColor.Value != sourceColor && allowCapturing)
                    {
                        resultCollection.Add(currentPosition);
                    }

                    break;
                }
            }
        }

        private static void GetPotentialCastlingMovePositions(
            IList<Piece> pieces,
            Position sourcePosition,
            PieceColor color,
            CastlingOptions castlingOptions,
            ICollection<Position> resultCollection)
        {
            switch (color)
            {
                case PieceColor.White:
                    {
                        GetPotentialCastlingMove(
                            pieces,
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.WhiteKingSide,
                            resultCollection);

                        GetPotentialCastlingMove(
                            pieces,
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.WhiteQueenSide,
                            resultCollection);
                    }
                    break;

                case PieceColor.Black:
                    {
                        GetPotentialCastlingMove(
                            pieces,
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.BlackKingSide,
                            resultCollection);

                        GetPotentialCastlingMove(
                            pieces,
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.BlackQueenSide,
                            resultCollection);
                    }
                    break;

                default:
                    throw color.CreateEnumValueNotSupportedException();
            }
        }

        private static bool CheckSquares(IList<Piece> pieces, Piece expectedPiece, IEnumerable<Position> positions)
        {
            return positions.All(position => GetPiece(pieces, position) == expectedPiece);
        }

        private static bool CheckSquares(IList<Piece> pieces, Piece expectedPiece, params Position[] positions)
        {
            return CheckSquares(pieces, expectedPiece, (IEnumerable<Position>)positions);
        }

        private static void GetPotentialCastlingMove(
            IList<Piece> pieces,
            Position sourcePosition,
            CastlingOptions castlingOptions,
            CastlingOptions option,
            ICollection<Position> resultCollection)
        {
            var castlingInfo = CastlingInfos[option];

            var isPotentiallyPossible = (castlingOptions & option) == option
                && sourcePosition == castlingInfo.CastlingMove.From
                && CheckSquares(pieces, Piece.None, castlingInfo.EmptySquares);

            if (isPotentiallyPossible)
            {
                resultCollection.Add(castlingInfo.CastlingMove.To);
            }
        }

        private static Position[] GetKingPotentialMovePositions(
            IList<Piece> pieces,
            CastlingOptions castlingOptions,
            Position sourcePosition,
            PieceColor pieceColor)
        {
            var resultList = new List<Position>();

            GetPotentialMovePositionsByRays(
                pieces,
                sourcePosition,
                pieceColor,
                KingAttackRays,
                MaxKingMoveDistance,
                true,
                resultList);

            GetPotentialCastlingMovePositions(
                pieces,
                sourcePosition,
                pieceColor,
                castlingOptions,
                resultList);

            return resultList.ToArray();
        }

        private static Position[] GetPawnPotentialMovePositions(
            IList<Piece> pieces,
            Position? enPassantCaptureTarget,
            Position sourcePosition,
            PieceColor pieceColor)
        {
            var resultList = new List<Position>();

            GetPotentialMovePositionsByRays(
                pieces,
                sourcePosition,
                pieceColor,
                PawnMoveRayMap[pieceColor].AsCollection(),
                MaxPawnAttackOrMoveDistance,
                false,
                resultList);

            var enPassantMove = GetEnPassantMove(pieces, sourcePosition);
            if (enPassantMove != null)
            {
                resultList.Add(enPassantMove.To);
            }

            var pawnAttackOffsets = PawnAttackOffsetMap[pieceColor];
            var attackPositions = GetOnboardPositions(sourcePosition, pawnAttackOffsets);
            var oppositeColor = pieceColor.Invert();

            //// ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var attackPosition in attackPositions)
            {
                var attackedPieceInfo = GetPieceInfo(pieces, attackPosition);
                if (attackedPieceInfo.Color == oppositeColor
                    || (enPassantCaptureTarget.HasValue && attackPosition == enPassantCaptureTarget.Value))
                {
                    resultList.Add(attackPosition);
                }
            }

            return resultList.ToArray();
        }

        #endregion
    }
}