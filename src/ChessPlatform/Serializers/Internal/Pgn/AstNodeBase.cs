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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IAstNodeInit.Init(AstContext context, ParseTreeNode parseNode)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (parseNode is null)
            {
                throw new ArgumentNullException(nameof(parseNode));
            }

            Initialize(context, parseNode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void AssertChildCount([NotNull] ParseTreeNode parseNode, int expectedChildCount)
        {
            var actualChildCount = parseNode.EnsureNotNull().ChildNodes.Count;
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
            if (!(parseNode.EnsureNotNull().ChildNodes[index].AstNode is TNode result))
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
                .EnsureNotNull()
                .ChildNodes
                .Select(obj => obj.EnsureNotNull().AstNode.EnsureNotNull())
                .Cast<TNode>()
                .ToArray();

            return result;
        }

        protected static string GetTokenText([NotNull] ParseTreeNode parseNode)
        {
            return parseNode.EnsureNotNull().Token.EnsureNotNull().Text.EnsureNotNull();
        }

        protected abstract void Initialize([NotNull] AstContext context, [NotNull] ParseTreeNode parseNode);
    }
}