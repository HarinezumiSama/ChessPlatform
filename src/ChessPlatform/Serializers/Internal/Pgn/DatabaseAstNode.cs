using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class DatabaseAstNode : AstNodeBase
    {
        public GameAstNode[] Games
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            Games = GetChildren<GameAstNode>(parseNode);
        }
    }
}