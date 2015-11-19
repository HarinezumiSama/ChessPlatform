using System;
using System.Linq;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public enum ElementType
    {
        MoveNumberIndication,
        SanMove,
        NumericAnnotationGlyph,
        RecursiveVariation
    }
}