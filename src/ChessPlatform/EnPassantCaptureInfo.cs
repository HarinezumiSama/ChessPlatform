using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    [DebuggerDisplay("{ToString(),nq}")]
    public sealed class EnPassantCaptureInfo : IEquatable<EnPassantCaptureInfo>
    {
        #region Constructors

        internal EnPassantCaptureInfo(Square captureSquare, Square targetPieceSquare)
        {
            CaptureSquare = captureSquare;
            TargetPieceSquare = targetPieceSquare;
        }

        #endregion

        #region Public Properties

        public Square CaptureSquare
        {
            get;
        }

        public Square TargetPieceSquare
        {
            get;
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals([CanBeNull] EnPassantCaptureInfo left, [CanBeNull] EnPassantCaptureInfo right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.CaptureSquare == right.CaptureSquare
                && left.TargetPieceSquare == right.TargetPieceSquare;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return Equals(obj as EnPassantCaptureInfo);
        }

        public override int GetHashCode()
        {
            return CaptureSquare.GetHashCode();
        }

        public override string ToString()
        {
            return
                $@"{{ {nameof(CaptureSquare)} = {CaptureSquare}, {nameof(TargetPieceSquare)} = {TargetPieceSquare
                    } }}";
        }

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==([CanBeNull] EnPassantCaptureInfo left, [CanBeNull] EnPassantCaptureInfo right)
        {
            return Equals(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=([CanBeNull] EnPassantCaptureInfo left, [CanBeNull] EnPassantCaptureInfo right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region IEquatable<EnPassantCaptureInfo> Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals([CanBeNull] EnPassantCaptureInfo other)
        {
            return Equals(this, other);
        }

        #endregion
    }
}