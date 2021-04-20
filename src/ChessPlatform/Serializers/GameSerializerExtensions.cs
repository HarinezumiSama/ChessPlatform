using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (gameDescriptions is null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            if (stringBuilder is null)
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
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (gameDescriptions is null)
            {
                throw new ArgumentNullException(nameof(gameDescriptions));
            }

            var stringBuilder = new StringBuilder();
            Serialize(serializer, gameDescriptions, stringBuilder);
            return stringBuilder.ToString();
        }

        public static GameDescription[] Deserialize([NotNull] this GameSerializer serializer, [NotNull] string data)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (data is null)
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