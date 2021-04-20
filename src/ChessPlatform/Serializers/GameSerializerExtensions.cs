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
        public static void Serialize(
            [NotNull] this GameSerializer serializer,
            [NotNull] ICollection<GameDescription> gameDescriptions,
            [NotNull] StringBuilder stringBuilder)
        {
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

            using (var writer = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                serializer.Serialize(gameDescriptions, writer);
            }
        }

        public static string Serialize(
            [NotNull] this GameSerializer serializer,
            [NotNull] ICollection<GameDescription> gameDescriptions)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (gameDescriptions == null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            var stringBuilder = new StringBuilder();
            Serialize(serializer, gameDescriptions, stringBuilder);
            return stringBuilder.ToString();
        }

        public static GameDescription[] Deserialize([NotNull] this GameSerializer serializer, [NotNull] string data)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var reader = new StringReader(data))
            {
                return serializer.Deserialize(reader);
            }
        }
    }
}