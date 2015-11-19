using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChessPlatform.Serializers.Internal.Pgn;
using Irony.Parsing;

namespace ChessPlatform.Serializers
{
    public sealed class PgnGameSerializer : GameSerializer
    {
        #region Protected Methods

        protected override void ExecuteSerialize(ICollection<GameDescription> gameDescriptions, TextWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override GameDescription[] ExecuteDeserialize(TextReader reader)
        {
            var sourceText = reader.ReadToEnd();

            var parser = new Parser(new PgnGrammar());
            var parseTree = parser.Parse(sourceText);

            if (parseTree.Status != ParseTreeStatus.Parsed)
            {
                var parserMessage = parseTree.ParserMessages.First();
                var errorLocation = parserMessage.Location;

                throw new ChessPlatformException(
                    $@"Invalid PGN. Error at line {errorLocation.Line + 1}, column {errorLocation.Column + 1}: {
                        parserMessage.Message}");
            }

            var databaseNode = (parseTree.Root.EnsureNotNull().AstNode as DatabaseAstNode).EnsureNotNull();
            foreach (var game in databaseNode.Games)
            {
                game.TagSection
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}