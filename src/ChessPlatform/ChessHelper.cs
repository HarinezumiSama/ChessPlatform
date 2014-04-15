using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
            new ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>>(
                new Dictionary<PieceColor, ReadOnlySet<byte>>
                {
                    { PieceColor.White, new byte[] { 0x0F, 0x11 }.ToHashSet().AsReadOnly() },
                    { PieceColor.Black, new byte[] { 0xEF, 0xF1 }.ToHashSet().AsReadOnly() }
                });

        public static readonly ReadOnlySet<byte> KingAttackOffsets =
            AllRays.Select(item => item.Offset).ToHashSet().AsReadOnly();

        public static readonly ReadOnlySet<byte> KnightAttackOffsets =
            new byte[] { 0x21, 0x1F, 0xE1, 0xDF, 0x12, 0x0E, 0xEE, 0xF2 }.ToHashSet().AsReadOnly();

        #endregion

        #region Public Methods

        public static Position[] GetKnightMovePositions(Position position)
        {
            return GetValidPositions(position, KnightAttackOffsets);
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

            return GetAttacks(pieces, kingPosition, kingColor.Invert()).Length != 0;
        }

        internal static Position? GetEnPassantTarget(IList<Piece> pieces, PieceMove move)
        {
            #region Argument Check

            ValidatePieces(pieces);

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var piece = GetPiece(pieces, move.From);
            var pieceType = piece.GetPieceType();
            var color = piece.GetColor();
            if (pieceType == PieceType.None || !color.HasValue)
            {
                throw new ArgumentException("The move starting position contains no piece.", "move");
            }

            if (pieceType != PieceType.Pawn || move.From.File != move.To.File)
            {
                return null;
            }

            if (color.Value == PieceColor.White
                && move.From.Rank == ChessConstants.WhiteEnPassantStartRank
                && move.To.Rank == ChessConstants.WhiteEnPassantEndRank)
            {
                return new Position(move.From.File, ChessConstants.WhiteEnPassantTargetRank);
            }

            if (color.Value == PieceColor.Black
                && move.From.Rank == ChessConstants.BlackEnPassantStartRank
                && move.To.Rank == ChessConstants.BlackEnPassantEndRank)
            {
                return new Position(move.From.File, ChessConstants.BlackEnPassantTargetRank);
            }

            return null;
        }

        internal static Position[] GetPotentialMovePositions(IList<Piece> pieces, Position sourcePosition)
        {
            #region Argument Check

            ValidatePieces(pieces);

            #endregion

            var piece = GetPiece(pieces, sourcePosition);

            var pieceType = piece.GetPieceType();
            var color = piece.GetColor();
            if (pieceType == PieceType.None || !color.HasValue)
            {
                throw new ArgumentException("No piece at the specified offset.", "sourcePosition");
            }

            if (pieceType == PieceType.Knight)
            {
                var result = GetKnightMovePositions(sourcePosition)
                    .Where(p => GetPiece(pieces, p).GetColor() != color.Value)
                    .ToArray();

                return result;
            }

            if (pieceType == PieceType.King)
            {
                //// TODO [vmcl] Implement GetPotentialMovePositions for King
                //// TODO [vmcl] Consider castling availability
                throw new NotImplementedException();
            }

            if (pieceType == PieceType.Pawn)
            {
                //// TODO [vmcl] Implement GetPotentialMovePositions for Pawn
                //// TODO [vmcl] Consider en passant move and capture
                throw new NotImplementedException();
            }

            var resultList = new List<Position>();

            if (pieceType.IsSlidingStraight())
            {
                GetPotentialMovePositionsByRays(pieces, sourcePosition, color.Value, StraightRays, resultList);
            }

            if (pieceType.IsSlidingDiagonally())
            {
                GetPotentialMovePositionsByRays(pieces, sourcePosition, color.Value, DiagonalRays, resultList);
            }

            return resultList.ToArray();
        }

        #endregion

        #region Private Methods

        private static Position[] GetValidPositions(Position position, ICollection<byte> x88Offsets)
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
            ICollection<Position> resultCollection)
        {
            foreach (var ray in rays)
            {
                for (var currentX88Value = (byte)(sourcePosition.X88Value + ray.Offset);
                    Position.IsValidX88Value(currentX88Value);
                    currentX88Value += ray.Offset)
                {
                    var currentPosition = new Position(currentX88Value);

                    var currentPiece = pieces[currentPosition.X88Value];
                    var currentColor = currentPiece.GetColor();
                    if (currentPiece == Piece.None || !currentColor.HasValue)
                    {
                        resultCollection.Add(currentPosition);
                        continue;
                    }

                    if (currentColor.Value != sourceColor)
                    {
                        resultCollection.Add(currentPosition);
                    }

                    break;
                }
            }
        }

        #endregion
    }
}