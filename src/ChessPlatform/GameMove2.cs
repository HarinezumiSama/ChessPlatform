using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public struct GameMove2 : IEquatable<GameMove2>
    {
        private const string FromGroupName = "from";
        private const string ToGroupName = "to";
        private const string PromotionGroupName = "promo";

        private const RegexOptions BasicPatternRegexOptions =
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace
                | RegexOptions.IgnoreCase;

        private static readonly string PromotionFenChars =
            new string(ChessConstants.GetValidPromotions().Select(item => item.GetFenChar()).ToArray());

        private static readonly Regex MainStringPatternRegex = new Regex(
            $@"^ (?<{FromGroupName}>[a-h][1-8]) (?:\-|x) (?<{ToGroupName}>[a-h][1-8]) (\=(?<{PromotionGroupName}>[{
                PromotionFenChars}]))? $",
            BasicPatternRegexOptions);

        private static readonly Regex UciStringPatternRegex = new Regex(
            $@"^ (?<{FromGroupName}>[a-h][1-8]) (?<{ToGroupName}>[a-h][1-8]) (?<{PromotionGroupName}>[{
                PromotionFenChars}])? $",
            BasicPatternRegexOptions);

        private static readonly Regex[] StringPatternRegexs = { MainStringPatternRegex, UciStringPatternRegex };

        private readonly int _hashCode;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMove2"/> class.
        /// </summary>
        public GameMove2(Square from, Square to, PieceType promotionResult)
        {
            From = from;
            To = to;
            PromotionResult = promotionResult;

            _hashCode = ((byte)PromotionResult << 16) | (To.SquareIndex << 8) | From.SquareIndex;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMove2"/> class.
        /// </summary>
        public GameMove2(Square from, Square to)
            : this(from, to, PieceType.None)
        {
            // Nothing to do
        }

        public Square From
        {
            [DebuggerStepThrough]
            get;
        }

        public Square To
        {
            [DebuggerStepThrough]
            get;
        }

        public PieceType PromotionResult
        {
            [DebuggerStepThrough]
            get;
        }

        public static bool operator ==(GameMove2 left, GameMove2 right) => Equals(left, right);

        public static bool operator !=(GameMove2 left, GameMove2 right) => !(left == right);

        [DebuggerNonUserCode]
        public static implicit operator GameMove2(string stringNotation) => FromStringNotation(stringNotation);

        public static bool Equals(GameMove2 left, GameMove2 right)
            => left.From == right.From && left.To == right.To && left.PromotionResult == right.PromotionResult;

        [DebuggerNonUserCode]
        public static GameMove2 FromStringNotation([NotNull] string stringNotation)
        {
            if (stringNotation == null)
            {
                throw new ArgumentNullException(nameof(stringNotation));
            }

            foreach (var stringPatternRegex in StringPatternRegexs)
            {
                var match = stringPatternRegex.Match(stringNotation);
                if (!match.Success)
                {
                    continue;
                }

                var from = match.Groups[FromGroupName].Value;
                var to = match.Groups[ToGroupName].Value;
                var promotionGroup = match.Groups[PromotionGroupName];

                var pieceType = promotionGroup.Success
                    ? char.ToUpperInvariant(promotionGroup.Value.Single()).ToPieceType()
                    : PieceType.None;

                return new GameMove2(Square.FromAlgebraic(@from), Square.FromAlgebraic(to), pieceType);
            }

            throw new ArgumentException(
                $@"Invalid string notation of a move: '{stringNotation}'.",
                nameof(stringNotation));
        }

        public override bool Equals(object obj) => obj is GameMove2 && Equals((GameMove2)obj);

        public override int GetHashCode() => _hashCode;

        public override string ToString() => ToString(false);

        public string ToString(bool renderCaptureSign)
            => $@"{From}{(renderCaptureSign ? ChessConstants.CaptureCharString : string.Empty)}{To}{
                (PromotionResult == PieceType.None
                    ? string.Empty
                    : ChessConstants.PromotionPrefixCharString + PromotionResult.GetFenChar())}";

        public GameMove2 MakePromotion(PieceType promotionResult)
        {
            if (!ChessConstants.ValidPromotions.Contains(promotionResult))
            {
                throw new ArgumentException(
                    $@"Must be a valid promotion piece ({promotionResult}).",
                    nameof(promotionResult));
            }

            return new GameMove2(From, To, promotionResult);
        }

        public GameMove2[] MakeAllPromotions()
        {
            var from = From;
            var to = To;

            return ChessConstants.ValidPromotions.Select(item => new GameMove2(from, to, item)).ToArray();
        }

        public bool Equals(GameMove2 other) => Equals(this, other);
    }
}