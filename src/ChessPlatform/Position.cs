﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public struct Position : IEquatable<Position>
    {
        #region Constants and Fields

        private readonly byte _file;
        private readonly byte _rank;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        [DebuggerNonUserCode]
        public Position(byte file, byte rank)
            : this(true, file, rank)
        {
            // Nothing to do
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        [DebuggerNonUserCode]
        public Position(byte x88Value)
        {
            #region Argument Check

            if (!IsValidX88Value(x88Value))
            {
                throw new ArgumentException("Out of the board 0x88 value.", "x88Value");
            }

            #endregion

            _file = (byte)(x88Value & 0x07);
            _rank = (byte)(x88Value >> 4);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        [DebuggerNonUserCode]
        private Position(bool checkArguments, byte file, byte rank)
        {
            #region Argument Check

            if (checkArguments)
            {
                if (!ChessConstants.FileRange.Contains(file))
                {
                    throw new ArgumentOutOfRangeException(
                        "file",
                        file,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The value is out of the valid range {0}.",
                            ChessConstants.FileRange));
                }

                if (!ChessConstants.RankRange.Contains(rank))
                {
                    throw new ArgumentOutOfRangeException(
                        "rank",
                        rank,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The value is out of the valid range {0}.",
                            ChessConstants.FileRange));
                }
            }

            #endregion

            _file = file;
            _rank = rank;
        }

        #endregion

        #region Public Properties

        public byte File
        {
            [DebuggerStepThrough]
            get
            {
                return _file;
            }
        }

        public byte Rank
        {
            [DebuggerStepThrough]
            get
            {
                return _rank;
            }
        }

        public byte X88Value
        {
            [DebuggerNonUserCode]
            get
            {
                return (byte)((_rank << 4) + _file);
            }
        }

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        public static implicit operator Position(string algebraicNotation)
        {
            return FromAlgebraic(algebraicNotation);
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public static Position FromAlgebraic(string algebraicNotation)
        {
            var position = TryFromAlgebraic(algebraicNotation);
            if (!position.HasValue)
            {
                throw new ArgumentException("Invalid algebraic notation.", "algebraicNotation");
            }

            return position.Value;
        }

        public static Position[] GenerateFile(byte file)
        {
            #region Argument Check

            if (!ChessConstants.FileRange.Contains(file))
            {
                throw new ArgumentOutOfRangeException(
                    "file",
                    file,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        ChessConstants.FileRange));
            }

            #endregion

            return Enumerable
                .Range(0, ChessConstants.RankCount)
                .Select(rank => new Position(file, (byte)rank))
                .ToArray();
        }

        public static Position[] GenerateFile(char file)
        {
            var fileValue = file - 'a';
            return GenerateFile(Convert.ToByte(fileValue));
        }

        public static Position[] GenerateRank(byte rank)
        {
            #region Argument Check

            if (!ChessConstants.RankRange.Contains(rank))
            {
                throw new ArgumentOutOfRangeException(
                    "rank",
                    rank,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        ChessConstants.FileRange));
            }

            #endregion

            return Enumerable
                .Range(0, ChessConstants.FileCount)
                .Select(file => new Position((byte)file, rank))
                .ToArray();
        }

        public override string ToString()
        {
            //// TODO [vmcl] Cache, if needed
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", (char)('a' + this.File), this.Rank + 1);
        }

        public override bool Equals(object obj)
        {
            return obj is Position && Equals((Position)obj);
        }

        public override int GetHashCode()
        {
            return _file.CombineHashCodes(_rank);
        }

        #endregion

        #region Internal Methods

        internal static bool IsValidX88Value(byte x88Value)
        {
            return (x88Value & 0x88) == 0;
        }

        [DebuggerNonUserCode]
        internal static Position? TryFromAlgebraic(string algebraicNotation)
        {
            if (algebraicNotation == null)
            {
                return null;
            }

            if (algebraicNotation.Length != 2)
            {
                return null;
            }

            var file = algebraicNotation[0] - 'a';
            var rank = algebraicNotation[1] - '1';

            return ChessConstants.FileRange.Contains(file) && ChessConstants.RankRange.Contains(rank)
                ? new Position(Convert.ToByte(file), Convert.ToByte(rank))
                : null;
        }

        #endregion

        #region IEquatable<Position> Members

        public bool Equals(Position other)
        {
            return other._file == _file && other._rank == _rank;
        }

        #endregion
    }
}