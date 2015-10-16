using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessPlatform
{
    public sealed class GameMove : IEquatable<GameMove>
    {
        #region Constants and Fields

        private const string FromGroupName = "from";
        private const string ToGroupName = "to";
        private const string PromotionGroupName = "promo";

        private const RegexOptions BasicPatternRegexOptions =
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace
                | RegexOptions.IgnoreCase;

        private static readonly Regex MainStringPatternRegex = new Regex(
            string.Format(
                CultureInfo.InvariantCulture,
                @"^ (?<{0}>[a-h][1-8]) (?:\-|x) (?<{1}>[a-h][1-8]) (\=(?<{2}>[{3}]))? $",
                FromGroupName,
                ToGroupName,
                PromotionGroupName,
                new string(ChessConstants.GetValidPromotions().Select(item => item.GetFenChar()).ToArray())),
            BasicPatternRegexOptions);

        private static readonly Regex UciStringPatternRegex = new Regex(
            string.Format(
                CultureInfo.InvariantCulture,
                @"^ (?<{0}>[a-h][1-8]) (?<{1}>[a-h][1-8]) (?<{2}>[{3}])? $",
                FromGroupName,
                ToGroupName,
                PromotionGroupName,
                new string(ChessConstants.GetValidPromotions().Select(item => item.GetFenChar()).ToArray())),
            BasicPatternRegexOptions);

        private static readonly Regex[] StringPatternRegexes = { MainStringPatternRegex, UciStringPatternRegex };

        private readonly int _hashCode;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMove"/> class.
        /// </summary>
        public GameMove(Position from, Position to, PieceType promotionResult)
        {
            From = from;
            To = to;
            PromotionResult = promotionResult;

            _hashCode = ((byte)PromotionResult << 16) | (To.X88Value << 8) | From.X88Value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMove"/> class.
        /// </summary>
        public GameMove(Position from, Position to)
            : this(from, to, PieceType.None)
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public Position From
        {
            get;
        }

        public Position To
        {
            get;
        }

        public PieceType PromotionResult
        {
            get;
        }

        #endregion

        #region Operators

        public static bool operator ==(GameMove left, GameMove right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GameMove left, GameMove right)
        {
            return !(left == right);
        }

        [DebuggerNonUserCode]
        public static implicit operator GameMove(string stringNotation)
        {
            return FromStringNotation(stringNotation);
        }

        #endregion

        #region Public Methods

        public static bool Equals(GameMove left, GameMove right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.From == right.From && left.To == right.To && left.PromotionResult == right.PromotionResult;
        }

        [DebuggerNonUserCode]
        public static GameMove FromStringNotation(string stringNotation)
        {
            #region Argument Check

            if (stringNotation == null)
            {
                throw new ArgumentNullException(nameof(stringNotation));
            }

            #endregion

            foreach (var stringPatternRegex in StringPatternRegexes)
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

                return new GameMove(Position.FromAlgebraic(@from), Position.FromAlgebraic(to), pieceType);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid string notation of a move '{0}'.",
                    stringNotation),
                nameof(stringNotation));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GameMove);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool renderCaptureSign)
        {
            var result = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}",
                From,
                renderCaptureSign ? "x" : string.Empty,
                To,
                PromotionResult == PieceType.None ? string.Empty : "=" + PromotionResult.GetFenChar());

            return result;
        }

        public GameMove MakePromotion(PieceType promotionResult)
        {
            #region Argument Check

            if (!ChessConstants.ValidPromotions.Contains(promotionResult))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Must be a valid promotion piece ({0}).",
                        promotionResult),
                    nameof(promotionResult));
            }

            #endregion

            return new GameMove(From, To, promotionResult);
        }

        public GameMove[] MakeAllPromotions()
        {
            return ChessConstants.ValidPromotions.Select(item => new GameMove(From, To, item)).ToArray();
        }

        #endregion

        #region IEquatable<GameMove> Members

        public bool Equals(GameMove other)
        {
            return Equals(this, other);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}