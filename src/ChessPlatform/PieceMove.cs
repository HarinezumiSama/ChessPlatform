using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChessPlatform
{
    public sealed class PieceMove : IEquatable<PieceMove>
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
        ///     Initializes a new instance of the <see cref="PieceMove"/> class.
        /// </summary>
        public PieceMove(Position from, Position to, PieceType promotionResult)
        {
            #region Argument Check

            if (from == to)
            {
                throw new ArgumentException("The source and destination positions cannot be the same.");
            }

            if (promotionResult != PieceType.None && !ChessConstants.ValidPromotions.Contains(promotionResult))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Invalid promotion '{0}'.", promotionResult),
                    "promotionResult");
            }

            #endregion

            this.From = from;
            this.To = to;
            this.PromotionResult = promotionResult;

            _hashCode = ((byte)this.PromotionResult << 16) | (this.To.X88Value << 8) | this.From.X88Value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMove"/> class.
        /// </summary>
        public PieceMove(Position from, Position to)
            : this(from, to, PieceType.None)
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public Position From
        {
            get;
            private set;
        }

        public Position To
        {
            get;
            private set;
        }

        public PieceType PromotionResult
        {
            get;
            private set;
        }

        #endregion

        #region Operators

        public static bool operator ==(PieceMove left, PieceMove right)
        {
            return EqualityComparer<PieceMove>.Default.Equals(left, right);
        }

        public static bool operator !=(PieceMove left, PieceMove right)
        {
            return !(left == right);
        }

        [DebuggerNonUserCode]
        public static implicit operator PieceMove(string stringNotation)
        {
            return FromStringNotation(stringNotation);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public static PieceMove FromStringNotation(string stringNotation)
        {
            #region Argument Check

            if (stringNotation == null)
            {
                throw new ArgumentNullException("stringNotation");
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

                return new PieceMove(Position.FromAlgebraic(@from), Position.FromAlgebraic(to), pieceType);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid string notation of a move '{0}'.",
                    stringNotation),
                "stringNotation");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PieceMove);
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
                this.From,
                renderCaptureSign ? "x" : "-",
                this.To,
                this.PromotionResult == PieceType.None ? string.Empty : "=" + this.PromotionResult.GetFenChar());

            return result;
        }

        public PieceMove MakePromotion(PieceType promotionResult)
        {
            #region Argument Check

            if (!ChessConstants.ValidPromotions.Contains(promotionResult))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Must be a valid promotion piece ({0}).",
                        promotionResult),
                    "promotionResult");
            }

            #endregion

            return new PieceMove(this.From, this.To, promotionResult);
        }

        public PieceMove[] MakeAllPromotions()
        {
            return ChessConstants.ValidPromotions.Select(item => new PieceMove(this.From, this.To, item)).ToArray();
        }

        #endregion

        #region IEquatable<PieceMove> Members

        public bool Equals(PieceMove other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.From == this.From && other.To == this.To && other.PromotionResult == this.PromotionResult;
        }

        #endregion

        #region Private Methods

        #endregion
    }
}