using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessPlatform
{
    internal static class SanMoveHelper
    {
        #region Constants and Fields

        private static readonly string ShortCastlingPattern = Regex.Escape(ShortCastlingSymbol);
        private static readonly string LongCastlingPattern = Regex.Escape(LongCastlingSymbol);

        private static readonly string CapturePattern = Regex.Escape(CaptureSymbol);
        private static readonly string CheckPattern = Regex.Escape(CheckSymbol);
        private static readonly string CheckmatePattern = Regex.Escape(CheckmateSymbol);

        internal static readonly string SanMovePattern =
            $@"(?<{MoveNotationGroupName}>(?:(?<{MovedPieceGroupName}>{PiecePattern}?)(?<{FromFileGroupName}>{
                FilePattern}?)(?<{FromRankGroupName}>{RankPattern}?)(?<{CaptureSignGroupName}>{CapturePattern}?)(?<{
                ToGroupName}>{FilePattern}{RankPattern})(?:\=(?<{PromotionGroupName}>{PromotionPattern}))?)|(?:{
                ShortCastlingPattern})|(?:{LongCastlingPattern}))(?<{CheckGroupName}>{CheckPattern}|{
                CheckmatePattern})?";

        internal static readonly Regex SanMoveRegex = new Regex(SanMovePattern, RegexOptions.Compiled);

        public const string MoveNotationGroupName = "MoveNotation";
        public const string MovedPieceGroupName = "MovedPiece";
        public const string FromFileGroupName = "FromFile";
        public const string FromRankGroupName = "FromRank";
        public const string CaptureSignGroupName = "CaptureSign";
        public const string ToGroupName = "To";
        public const string PromotionGroupName = "Promotion";
        public const string CheckGroupName = "Check";

        public const string ShortCastlingSymbol = "O-O";
        public const string LongCastlingSymbol = "O-O-O";

        public const string CaptureSymbol = "x";
        public const string CheckSymbol = "+";
        public const string CheckmateSymbol = "#";

        private const string FilePattern = "[abcdefgh]";
        private const string RankPattern = "[12345678]";
        private const string PiecePattern = "[KQRBNP]"; // Though not recommended, P may be used for pawn
        private const string PromotionPattern = "[QRBN]";

        #endregion
    }
}