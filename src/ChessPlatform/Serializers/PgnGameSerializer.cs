using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            throw new NotImplementedException();
        }

        #endregion
    }
}