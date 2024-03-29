﻿using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class GameMove2Extensions
    {
        private const string MoveSeparator = ", ";

        public static string ToUciNotation(this GameMove2 move)
        {
            var isPromotion = move.PromotionResult != PieceType.None;

            var chars = new[]
            {
                move.From.FileChar,
                move.From.RankChar,
                move.To.FileChar,
                move.To.RankChar,
                isPromotion ? char.ToLowerInvariant(move.PromotionResult.GetFenChar()) : '\0'
            };

            return new string(chars, 0, isPromotion ? chars.Length : chars.Length - 1);
        }

        public static string ToUciNotation([NotNull] this ICollection<GameMove2> moves)
        {
            if (moves is null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            return moves.Select(ToUciNotation).Join(MoveSeparator);
        }
    }
}