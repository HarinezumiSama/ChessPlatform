using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChessPlatform.Serializers
{
    public sealed class FenGameSerializer : GameSerializer
    {
        protected override void ExecuteSerialize(ICollection<GameDescription> gameDescriptions, TextWriter writer)
        {
            //// ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var gameDescription in gameDescriptions)
            {
                var finalBoard = gameDescription.FinalBoard;
                var fen = finalBoard.GetFen();
                writer.WriteLine(fen);
            }
        }

        protected override GameDescription[] ExecuteDeserialize(TextReader reader)
        {
            var gameDescriptions = new List<GameDescription>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var fen = line.Trim();
                var gameBoard = new GameBoard(fen);
                var gameDescription = new GameDescription(gameBoard);
                gameDescriptions.Add(gameDescription);
            }

            return gameDescriptions.ToArray();
        }
    }
}