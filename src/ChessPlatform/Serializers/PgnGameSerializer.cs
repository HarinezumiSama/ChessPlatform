using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChessPlatform.Serializers.Internal;
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

            Trace.WriteLine($@"Parse status = {parseTree.Status}.");

            throw new NotImplementedException();
        }

        #endregion
    }
}