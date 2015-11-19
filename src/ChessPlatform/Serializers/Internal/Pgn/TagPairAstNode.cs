using System;
using System.Diagnostics;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("[{GetType().Name,nq}] Name = {Name.Text}, Value = {Value.Text}")]
    public sealed class TagPairAstNode : AstNodeBase
    {
        #region Public Properties

        public TagNameAstNode Name
        {
            get;
            private set;
        }

        public TagValueAstNode Value
        {
            get;
            private set;
        }

        #endregion

        #region Protected Methods

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 2);

            Name = GetChildNode<TagNameAstNode>(parseNode, 0);
            Value = GetChildNode<TagValueAstNode>(parseNode, 1);
        }

        #endregion
    }
}