﻿using System.Diagnostics;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("[{GetType().Name,nq}] Name = {Name.Text}, Value = {Value.Text}")]
    public sealed class TagPairAstNode : AstNodeBase
    {
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

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            AssertChildCount(parseNode, 2);

            Name = GetChildNode<TagNameAstNode>(parseNode, 0);
            Value = GetChildNode<TagValueAstNode>(parseNode, 1);
        }
    }
}