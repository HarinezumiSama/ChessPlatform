using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Irony.Ast;
using Irony.Parsing;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public abstract class AstNodeBase : IAstNodeInit
    {
        #region IAstNodeInit Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IAstNodeInit.Init(AstContext context, ParseTreeNode parseNode)
        {
            #region Argument Check

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (parseNode == null)
            {
                throw new ArgumentNullException(nameof(parseNode));
            }

            #endregion

            Initialize(context, parseNode);
        }

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void AssertChildCount([NotNull] ParseTreeNode parseNode, int expectedChildCount)
        {
            var actualChildCount = parseNode.ChildNodes.Count;
            if (actualChildCount != expectedChildCount)
            {
                throw new InvalidOperationException(
                    $@"The expected child node count is {expectedChildCount} while actual is {actualChildCount}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        [Pure]
        protected static TNode GetChildNode<TNode>([NotNull] ParseTreeNode parseNode, int index)
            where TNode : AstNodeBase
        {
            var result = parseNode.ChildNodes[index].AstNode as TNode;
            if (result == null)
            {
                throw new InvalidOperationException(
                    $@"The child node at index {index} is expected to be of type '{
                        typeof(TNode).GetQualifiedName()}'.");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        [Pure]
        protected static TNode[] GetChildren<TNode>([NotNull] ParseTreeNode parseNode)
            where TNode : AstNodeBase
        {
            var result = parseNode
                .ChildNodes
                .Select(obj => obj.EnsureNotNull().AstNode.EnsureNotNull())
                .Cast<TNode>()
                .ToArray();

            return result;
        }

        protected abstract void Initialize([NotNull] AstContext context, [NotNull] ParseTreeNode parseNode);

        #endregion
    }
}