using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    [DebuggerDisplay("{ToString(),nq}")]
    public sealed class EnPassantCaptureInfo : IEquatable<EnPassantCaptureInfo>
    {
        internal EnPassantCaptureInfo(Square captureSquare, Square targetPieceSquare)
        {
            CaptureSquare = captureSquare;
            TargetPieceSquare = targetPieceSquare;
        }

        public Square CaptureSquare
        {
            get;
        }

        public Square TargetPieceSquare
        {
            get;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals([CanBeNull] EnPassantCaptureInfo left, [CanBeNull] EnPassantCaptureInfo right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EnPassantCaptureInfo other)
        {
            return Equals(this, other);
        }
    }
}