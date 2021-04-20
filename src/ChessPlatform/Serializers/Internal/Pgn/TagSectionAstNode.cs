using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public sealed class TagSectionAstNode : AstNodeBase
    {
        private Dictionary<string, string> _tagMap;

        public TagPairAstNode[] TagPairs
        {
            get;
            private set;
        }

        [CanBeNull]
        public string GetTagValue([NotNull] string tagName)
        {
            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            return _tagMap.GetValueOrDefault(tagName);
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            TagPairs = GetChildren<TagPairAstNode>(parseNode);
            _tagMap = TagPairs.ToDictionary(node => node.Name.Text, node => node.Value.Text);
        }
    }
}