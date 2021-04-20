using System;
using System.Diagnostics;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("{DebugText,nq}")]
    public sealed class ElementSequenceAstNode : AstNodeBase
    {
        public ElementAstNode[] Elements
        {
            get;
            private set;
        }

        public string DebugText => $@"[{GetType().Name}] Elements.Length = {Elements?.Length}";

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            Elements = GetChildren<ElementAstNode>(parseNode);
        }
    }
}