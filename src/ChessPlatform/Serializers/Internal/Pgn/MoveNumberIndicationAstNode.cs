using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Irony.Ast;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    [DebuggerDisplay("[{GetType().Name,nq}] MoveNumber = {MoveNumber}")]
    public sealed class MoveNumberIndicationAstNode : AstNodeBase
    {
        private static readonly Regex MoveNumberRegex = new Regex(@"\d+", RegexOptions.Compiled);

        public int MoveNumber
        {
            get;
            private set;
        }

        protected override void Initialize(AstContext context, ParseTreeNode parseNode)
        {
            var tokenText = GetTokenText(parseNode);
            var match = MoveNumberRegex.Match(tokenText);
            if (!match.Success)
            {
                throw new InvalidOperationException($@"Invalid move number indication '{tokenText}'.");
            }

            MoveNumber = int.Parse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}