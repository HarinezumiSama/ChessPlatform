using System.Diagnostics;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("{ToDebuggerString(),nq}")]
    public sealed class ElementSequenceAstNode : AstNodeBase
    {
        public ElementAstNode[] Elements { get; private set; }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            Elements = GetChildren<ElementAstNode>(parseNode);
        }

        private string ToDebuggerString() => $@"[{GetType().Name}] {nameof(Elements)}.{nameof(Elements.Length)} = {Elements?.Length}";
    }
}