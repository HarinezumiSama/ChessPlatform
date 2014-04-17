﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public static class ChessHelper
    {
        #region Constants and Fields

        public static readonly ReadOnlyDictionary<CastlingOptions, CastlingInfo> CastlingOptionToInfoMap =
            ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.Option).AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>
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

        public static readonly ReadOnlyDictionary<PieceColor, byte> ColorToPawnPromotionRankMap =
            new ReadOnlyDictionary<PieceColor, byte>(
                new Dictionary<PieceColor, byte>
                {
                    { PieceColor.White, ChessConstants.WhitePawnPromotionRank },
                    { PieceColor.Black, ChessConstants.BlackPawnPromotionRank }
                });

        public static readonly ReadOnlySet<Position> AllPositions =
            Enumerable
                .Range(0, ChessConstants.FileCount)
                .SelectMany(rank => Position.GenerateRank((byte)rank))
                .ToHashSet()
                .AsReadOnly();

        internal const int MaxSlidingPieceDistance = 8;
        internal const int MaxPawnAttackOrMoveDistance = 1;
        internal const int MaxKingMoveDistance = 1;

        internal static readonly ReadOnlyCollection<RayInfo> StraightRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0xFF, true),
                    new RayInfo(0x01, true),
                    new RayInfo(0xF0, true),
                    new RayInfo(0x10, true)
                });

        internal static readonly ReadOnlyCollection<RayInfo> DiagonalRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0x0F, false),
                    new RayInfo(0x11, false),
                    new RayInfo(0xEF, false),
                    new RayInfo(0xF1, false)
                });

        internal static readonly ReadOnlyCollection<RayInfo> AllRays =
            new ReadOnlyCollection<RayInfo>(StraightRays.Concat(DiagonalRays).ToArray());

        internal static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x10, true) },
                    { PieceColor.Black, new RayInfo(0xF0, true) }
                });

        internal static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnEnPassantMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x20, true) },
                    { PieceColor.Black, new RayInfo(0xE0, true) }
                });

        internal static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnAttackRayMap =
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

        internal static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
            PawnAttackRayMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(item => item.Offset).ToHashSet().AsReadOnly()).AsReadOnly();

        internal static readonly ReadOnlySet<RayInfo> KingAttackRays = AllRays.ToHashSet().AsReadOnly();

        internal static readonly ReadOnlySet<byte> KingAttackOrMoveOffsets =
            KingAttackRays.Select(item => item.Offset).ToHashSet().AsReadOnly();

        internal static readonly ReadOnlySet<byte> KnightAttackOrMoveOffsets =
            new byte[] { 0x21, 0x1F, 0xE1, 0xDF, 0x12, 0x0E, 0xEE, 0xF2 }.ToHashSet().AsReadOnly();

        #endregion

        #region Public Methods

        public static Position[] GetKnightMovePositions(Position position)
        {
            return GetOnboardPositions(position, KnightAttackOrMoveOffsets);
        }

        #endregion

        #region Internal Methods

        internal static Position[] GetOnboardPositions(Position position, ICollection<byte> x88Offsets)
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

        #endregion
    }
}