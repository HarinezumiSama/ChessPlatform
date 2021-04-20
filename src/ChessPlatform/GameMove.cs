using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class GameMove : IEquatable<GameMove>
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
        ///     Initializes a new instance of the <see cref="GameMove"/> class.
        /// </summary>
        public GameMove(Square from, Square to, PieceType promotionResult)
        {
            From = from;
            To = to;
            PromotionResult = promotionResult;

            _hashCode = ((byte)PromotionResult << 16) | (To.SquareIndex << 8) | From.SquareIndex;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMove"/> class.
        /// </summary>
        public GameMove(Square from, Square to)
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

        public static bool operator ==(GameMove left, GameMove right) => Equals(left, right);

        public static bool operator !=(GameMove left, GameMove right) => !(left == right);

        [DebuggerNonUserCode]
        public static implicit operator GameMove(string stringNotation) => FromStringNotation(stringNotation);

        public static bool Equals(GameMove left, GameMove right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.From == right.From && left.To == right.To && left.PromotionResult == right.PromotionResult;
        }

        [DebuggerNonUserCode]
        [NotNull]
        public static GameMove FromStringNotation([NotNull] string stringNotation)
        {
            if (stringNotation is null)
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

                return new GameMove(Square.FromAlgebraic(from), Square.FromAlgebraic(to), pieceType);
            }

            throw new ArgumentException(
                $@"Invalid string notation of a move: '{stringNotation}'.",
                nameof(stringNotation));
        }

        public override bool Equals(object obj) => Equals(obj as GameMove);

        public override int GetHashCode() => _hashCode;

        public override string ToString() => ToString(false);

        public string ToString(bool renderCaptureSign)
        {
            var result = $@"{From}{(renderCaptureSign ? ChessConstants.CaptureCharString : string.Empty)}{To}{
                (PromotionResult == PieceType.None
                    ? string.Empty
                    : ChessConstants.PromotionPrefixCharString + PromotionResult.GetFenChar())}";

            return result;
        }

        public GameMove MakePromotion(PieceType promotionResult)
        {
            if (!ChessConstants.ValidPromotions.Contains(promotionResult))
            {
                throw new ArgumentException(
                    $@"Must be a valid promotion piece ({promotionResult}).",
                    nameof(promotionResult));
            }

            return new GameMove(From, To, promotionResult);
        }

        public GameMove[] MakeAllPromotions()
            => ChessConstants.ValidPromotions.Select(item => new GameMove(From, To, item)).ToArray();

        public bool Equals(GameMove other) => Equals(this, other);
    }
}