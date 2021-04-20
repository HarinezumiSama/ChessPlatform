using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class RecursiveVariationAstNode : AstNodeBase
    {
        public ElementSequenceAstNode ElementSequence
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 1);

            ElementSequence = GetChildNode<ElementSequenceAstNode>(parseNode, 0);
        }
    }
}