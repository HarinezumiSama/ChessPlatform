using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class MovetextSectionAstNode : AstNodeBase
    {
        public ElementSequenceAstNode ElementSequence
        {
            get;
            private set;
        }

        public GameTerminationAstNode GameTermination
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 2);

            ElementSequence = GetChildNode<ElementSequenceAstNode>(parseNode, 0);
            GameTermination = GetChildNode<GameTerminationAstNode>(parseNode, 1);
        }
    }
}