using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Omnifactotum.Annotations;

namespace ChessPlatform.Serializers
{
    public static class GameSerializerExtensions
    {
        #region Public Methods

        public static void Serialize(
            [NotNull] this GameSerializer serializer,
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] StringBuilder stringBuilder)
        {
            #region Argument Check

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (gameDescriptions == null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            if (stringBuilder == null)
            {
                throw new ArgumentNullException(nameof(stringBuilder));
            }

            #endregion

            using (var writer = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                serializer.Serialize(gameDescriptions, writer);
            }
        }

        public static string Serialize(
            [NotNull] this GameSerializer serializer,
            [NotNull] ICollection<GameDescription> gameDescriptions)
        {
            #region Argument Check

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (gameDescriptions == null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            #endregion

            var stringBuilder = new StringBuilder();
            Serialize(serializer, gameDescriptions, stringBuilder);
            return stringBuilder.ToString();
        }

        public static GameDescription[] Deserialize([NotNull] this GameSerializer serializer, [NotNull] string data)
        {
            #region Argument Check

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            #endregion

            using (var reader = new StringReader(data))
            {
                return serializer.Deserialize(reader);
            }
        }

        #endregion
    }
}