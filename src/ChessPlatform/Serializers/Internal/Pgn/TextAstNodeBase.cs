using System;
using System.Diagnostics;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("[{GetType().Name,nq}] Text = {Text}")]
    public abstract class TextAstNodeBase : AstNodeBase
    {
        public string Text
        {
            get;
            private set;
        }

        protected sealed override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            var tokenText = GetTokenText(parseNode);
            Text = ExtractText(tokenText);
        }

        protected virtual string ExtractText(string tokenText)
        {
            return tokenText;
        }
    }
}