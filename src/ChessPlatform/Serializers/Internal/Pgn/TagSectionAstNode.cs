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
        #region Constants and Fields

        private Dictionary<string, string> _tagMap;

        #endregion

        #region Public Properties

        public TagPairAstNode[] TagPairs
        {
            get;
            private set;
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public string GetTagValue([NotNull] string tagName)
        {
            #region Argument Check

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            #endregion

            return _tagMap.GetValueOrDefault(tagName);
        }

        #endregion

        #region Protected Methods

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            TagPairs = GetChildren<TagPairAstNode>(parseNode);
            _tagMap = TagPairs.ToDictionary(node => node.Name.Text, node => node.Value.Text);
        }

        #endregion
    }
}