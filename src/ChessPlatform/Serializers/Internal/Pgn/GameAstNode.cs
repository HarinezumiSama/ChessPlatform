using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class GameAstNode : AstNodeBase
    {
        public MovetextSectionAstNode MovetextSection
        {
            get;
            private set;
        }

        public TagSectionAstNode TagSection
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 2);

            TagSection = GetChildNode<TagSectionAstNode>(parseNode, 0);
            MovetextSection = GetChildNode<MovetextSectionAstNode>(parseNode, 1);
        }
    }
}