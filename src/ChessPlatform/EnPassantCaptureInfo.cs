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

        internal EnPassantCaptureInfo(Position capturePosition, Position targetPiecePosition)
        {
            CapturePosition = capturePosition;
            TargetPiecePosition = targetPiecePosition;
        }

        #endregion

        #region Public Properties

        public Position CapturePosition
        {
            get;
        }

        public Position TargetPiecePosition
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

            return left.CapturePosition == right.CapturePosition
                && left.TargetPiecePosition == right.TargetPiecePosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return Equals(obj as EnPassantCaptureInfo);
        }

        public override int GetHashCode()
        {
            return CapturePosition.GetHashCode();
        }

        public override string ToString()
        {
            return $@"{{ CapturePosition = {CapturePosition}, TargetPiecePosition = {TargetPiecePosition} }}";
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