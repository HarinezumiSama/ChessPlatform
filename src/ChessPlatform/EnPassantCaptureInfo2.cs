using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

//// EnPassantCaptureInfo2 - NEW: instead of EnPassantCaptureInfo

namespace ChessPlatform
{
    //// ReSharper disable once UseNameofExpression - False positive
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct EnPassantCaptureInfo2 : IEquatable<EnPassantCaptureInfo2>
    {
        internal EnPassantCaptureInfo2(Square captureSquare, Square targetPieceSquare)
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
        public static bool Equals(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right)
            => left.CaptureSquare == right.CaptureSquare && left.TargetPieceSquare == right.TargetPieceSquare;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is EnPassantCaptureInfo2 && Equals((EnPassantCaptureInfo2)obj);

        public override int GetHashCode() => CaptureSquare.GetHashCode();

        public override string ToString()
            => $@"{{ {nameof(CaptureSquare)} = {CaptureSquare}, {nameof(TargetPieceSquare)} = {TargetPieceSquare} }}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right) => Equals(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right) => !Equals(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EnPassantCaptureInfo2 other) => Equals(this, other);
    }
}