using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class ElementAstNode : AstNodeBase
    {
        public ElementType? ElementType
        {
            get;
            private set;
        }

        public MoveNumberIndicationAstNode MoveNumberIndication
        {
            get;
            private set;
        }

        public SanMoveAstNode SanMove
        {
            get;
            private set;
        }

        public NumericAnnotationGlyphAstNode NumericAnnotationGlyph
        {
            get;
            private set;
        }

        public RecursiveVariationAstNode RecursiveVariation
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 1);

            var childNode = parseNode.ChildNodes[0].AstNode.EnsureNotNull();

            MoveNumberIndication = childNode as MoveNumberIndicationAstNode;
            SetElementTypeIfNodeNotNull(MoveNumberIndication, Pgn.ElementType.MoveNumberIndication);

            SanMove = childNode as SanMoveAstNode;
            SetElementTypeIfNodeNotNull(SanMove, Pgn.ElementType.SanMove);

            NumericAnnotationGlyph = childNode as NumericAnnotationGlyphAstNode;
            SetElementTypeIfNodeNotNull(NumericAnnotationGlyph, Pgn.ElementType.NumericAnnotationGlyph);

            RecursiveVariation = childNode as RecursiveVariationAstNode;
            SetElementTypeIfNodeNotNull(RecursiveVariation, Pgn.ElementType.RecursiveVariation);

            if (!ElementType.HasValue)
            {
                throw new NotImplementedException(
                    $@"Unexpected node type: {childNode.GetType().GetQualifiedName()}.");
            }
        }

        private void SetElementTypeIfNodeNotNull([CanBeNull] AstNodeBase node, ElementType elementType)
        {
            if (node != null)
            {
                ElementType = elementType;
            }
        }
    }
}