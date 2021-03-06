﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

//// EnPassantCaptureInfo2 - NEW: instead of EnPassantCaptureInfo

namespace ChessPlatform
{
    //// ReSharper disable once UseNameofExpression - False positive
    [DebuggerDisplay("{ToString(),nq}")]
    public struct EnPassantCaptureInfo2 : IEquatable<EnPassantCaptureInfo2>
    {
        #region Constructors

        internal EnPassantCaptureInfo2(Square captureSquare, Square targetPieceSquare)
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
        public static bool Equals(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right)
            => left.CaptureSquare == right.CaptureSquare && left.TargetPieceSquare == right.TargetPieceSquare;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is EnPassantCaptureInfo2 && Equals((EnPassantCaptureInfo2)obj);

        public override int GetHashCode() => CaptureSquare.GetHashCode();

        public override string ToString()
            => $@"{{ {nameof(CaptureSquare)} = {CaptureSquare}, {nameof(TargetPieceSquare)} = {TargetPieceSquare} }}";

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right) => Equals(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EnPassantCaptureInfo2 left, EnPassantCaptureInfo2 right) => !Equals(left, right);

        #endregion

        #region IEquatable<EnPassantCaptureData> Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EnPassantCaptureInfo2 other) => Equals(this, other);

        #endregion
    }
}