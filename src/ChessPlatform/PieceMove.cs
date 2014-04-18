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

        private static readonly Regex StringPatternRegex = new Regex(
            string.Format(
                CultureInfo.InvariantCulture,
                @"(?<{0}>[a-h][1-8])\-(?<{1}>[a-h][1-8])",
                FromGroupName,
                ToGroupName),
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMove"/> class.
        /// </summary>
        public PieceMove(Position from, Position to)
        {
            #region Argument Check

            if (from == to)
            {
                throw new ArgumentException("The source and destination positions must be different.");
            }

            #endregion

            this.From = from;
            this.To = to;
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

            if (string.IsNullOrEmpty(stringNotation))
            {
                throw new ArgumentException(@"The value can be neither empty string nor null.", "stringNotation");
            }

            #endregion

            var match = StringPatternRegex.Match(stringNotation);
            if (!match.Success)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid string notation of a move '{0}'.",
                        stringNotation),
                    "stringNotation");
            }

            var from = match.Groups[FromGroupName].Value;
            var to = match.Groups[ToGroupName].Value;
            return new PieceMove(Position.FromAlgebraic(from), Position.FromAlgebraic(to));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PieceMove);
        }

        public override int GetHashCode()
        {
            return this.From.CombineHashCodes(this.To);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", this.From, this.To);
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

            return other.From == this.From && other.To == this.To;
        }

        #endregion
    }
}