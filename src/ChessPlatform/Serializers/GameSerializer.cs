using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers
{
    public abstract class GameSerializer
    {
        #region Public Methods

        public void Serialize(
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] TextWriter writer)
        {
            #region Argument Check

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

            #endregion

            ExecuteSerialize(gameDescriptions, writer);
        }

        public GameDescription[] Deserialize([NotNull] TextReader reader)
        {
            #region Argument Check

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            #endregion

            return ExecuteDeserialize(reader);
        }

        #endregion

        #region Protected Methods

        protected abstract void ExecuteSerialize(
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] TextWriter writer);

        protected abstract GameDescription[] ExecuteDeserialize([NotNull] TextReader reader);

        #endregion
    }
}