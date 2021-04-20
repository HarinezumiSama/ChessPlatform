using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers
{
    public abstract class GameSerializer
    {
        public void Serialize(
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] TextWriter writer)
        {
            if (gameDescriptions == null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            if (gameDescriptions.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(gameDescriptions));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            ExecuteSerialize(gameDescriptions, writer);
        }

        public GameDescription[] Deserialize([NotNull] TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return ExecuteDeserialize(reader);
        }

        protected abstract void ExecuteSerialize(
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] TextWriter writer);

        protected abstract GameDescription[] ExecuteDeserialize([NotNull] TextReader reader);
    }
}