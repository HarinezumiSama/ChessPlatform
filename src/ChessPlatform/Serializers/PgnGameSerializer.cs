using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChessPlatform.Serializers.Internal.Pgn;
using Irony.Parsing;

namespace ChessPlatform.Serializers
{
    public sealed class PgnGameSerializer : GameSerializer
    {
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
            var games = databaseNode.Games.EnsureNotNull();

            var resultList = new List<GameDescription>(games.Length);
            foreach (var game in games)
            {
                var fen = game.TagSection.GetTagValue(TagNames.Fen);
                var initialBoard = fen == null ? new GameBoard() : new GameBoard(fen, true);

                var moves = new List<GameMove>(game.MovetextSection.ElementSequence.Elements.Length);
                var currentBoard = initialBoard;
                foreach (var element in game.MovetextSection.ElementSequence.Elements)
                {
                    if (!element.ElementType.HasValue || element.ElementType.Value != ElementType.SanMove)
                    {
                        continue;
                    }

                    var sanMoveText = element.SanMove.EnsureNotNull().Text.EnsureNotNull();
                    var move = currentBoard.ParseSanMove(sanMoveText);
                    moves.Add(move);
                    currentBoard = currentBoard.MakeMove(move);
                }

                var gameDescription = new GameDescription(initialBoard, moves);
                resultList.Add(gameDescription);
            }

            return resultList.ToArray();
        }
    }
}