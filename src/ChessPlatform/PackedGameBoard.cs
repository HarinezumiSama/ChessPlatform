using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class PackedGameBoard : IEquatable<PackedGameBoard>
    {
        #region Constants and Fields

        private const int PositionCount = 64;
        private const int BitsPerPosition = 4;

        private const int Length = BitsPerPosition * PositionCount;
        private const int Size = (Length + 7) / 8;

        private readonly byte[] _pieces;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PackedGameBoard"/> class.
        /// </summary>
        public PackedGameBoard([NotNull] GameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            _pieces = PopulatePieces(board);
            this.ActiveColor = board.ActiveColor;
            this.CastlingOptions = board.CastlingOptions;

            this.EnPassantMoveCapturePositionIndex =
                board.EnPassantCaptureInfo == null
                    ? -1
                    : board.EnPassantCaptureInfo.CapturePosition.Bitboard.FindFirstBitSet();

            this.HashCode = _pieces.ComputeCollectionHashCode()
                .CombineHashCodes(ActiveColor)
                .CombineHashCodes(CastlingOptions)
                .CombineHashCodes(EnPassantMoveCapturePositionIndex);
        }

        #endregion

        #region Internal Properties

        internal PieceColor ActiveColor
        {
            get;
            private set;
        }

        internal CastlingOptions CastlingOptions
        {
            get;
            private set;
        }

        internal int EnPassantMoveCapturePositionIndex
        {
            get;
            private set;
        }

        internal int HashCode
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as PackedGameBoard);
        }

        public override int GetHashCode()
        {
            return this.HashCode;
        }

        #endregion

        #region IEquatable<PackedGameBoard> Members

        public bool Equals(PackedGameBoard other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.HashCode == this.HashCode
                && other.ActiveColor == this.ActiveColor
                && other.CastlingOptions == this.CastlingOptions
                && other.EnPassantMoveCapturePositionIndex == this.EnPassantMoveCapturePositionIndex
                && ByteArraysEqual(other._pieces, _pieces);
        }

        #endregion

        #region Internal Methods

        internal byte[] GetPieces()
        {
            return _pieces.Copy();
        }

        #endregion

        #region Private Methods

        //// TODO [vmcl] Optimize this dummy implementation
        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            var length = left.EnsureNotNull().Length;
            if (length != right.EnsureNotNull().Length)
            {
                return false;
            }

            for (var index = 0; index < length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] PopulatePieces([NotNull] IGameBoard board)
        {
            var pieces = new byte[Size];

            for (int index = 0, bitOffset = 0; index < PositionCount; index++, bitOffset += BitsPerPosition)
            {
                var position = Position.FromBitboardBitIndex(index);
                var pieceValue = (int)board[position];

                //// TODO [vmcl] Remove this temporary verification
                if (pieceValue >= (1 << BitsPerPosition))
                {
                    throw new InvalidOperationException("Piece value size is too large.");
                }

                var byteOffset = bitOffset >> 3;
                var value = (byte)((pieceValue & 0x0F) << (bitOffset & 0x7));
                pieces[byteOffset] |= value;
            }

            return pieces;
        }

        #endregion
    }
}