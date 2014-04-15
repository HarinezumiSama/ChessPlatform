using System;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceTypeMask
    {
        public const PieceType Sliding = (PieceType)0x04;

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
        public const PieceType SlidingDiagonally = Sliding | (PieceType)0x01;

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
        public const PieceType SlidingStraight = Sliding | (PieceType)0x02;

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
        public const PieceType SlidingAllWays = SlidingDiagonally | SlidingStraight;
    }
}