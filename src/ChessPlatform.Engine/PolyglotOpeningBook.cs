using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ChessPlatform.Engine.Properties;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public sealed class PolyglotOpeningBook : IOpeningBook
    {
        private static readonly OpeningGameMove[] NoMoves = new OpeningGameMove[0];

        private static readonly Lazy<PolyglotOpeningBook> PerformanceInstance = Lazy.Create(
            () => InitializeBook(() => Resources.OpeningBook_Performance_Polyglot),
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<PolyglotOpeningBook> VariedInstance = Lazy.Create(
            () => InitializeBook(() => Resources.OpeningBook_Varied_Polyglot),
            LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly Dictionary<long, BookEntry[]> _entryMap;

        public PolyglotOpeningBook([NotNull] Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(@"The stream cannot be read.", nameof(stream));
            }

            var capacity = 128; // Guesstimate
            if (stream.CanSeek)
            {
                var streamLength = stream.Length - stream.Position;
                var quotient = Math.DivRem(streamLength, BookEntry.DataLength, out var remainder);

                if (remainder != 0)
                {
                    throw new ArgumentException(
                        $@"Invalid opening book stream length ({streamLength}).",
                        nameof(stream));
                }

                capacity = (int)Math.Min(quotient, int.MaxValue);
            }

            var entries = new List<BookEntry>(capacity);

            while (true)
            {
                var entry = BookEntry.ReadEntry(stream);
                if (!entry.HasValue)
                {
                    break;
                }

                entries.Add(entry.Value);
            }

            _entryMap = entries
                .ToLookup(entry => entry.Key)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
        }

        public static PolyglotOpeningBook Performance => PerformanceInstance.Value;

        public static PolyglotOpeningBook Varied => VariedInstance.Value;

        public OpeningGameMove[] FindPossibleMoves(GameBoard board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var key = board.ZobristKey;
            var entries = _entryMap.GetValueOrDefault(key);

            var result = entries?
                .Select(obj => new OpeningGameMove(obj.Move, obj.Weight, obj.Learn))
                .Where(obj => board.ValidMoves.ContainsKey(obj.Move))
                .OrderByDescending(obj => obj.Weight)
                .ToArray()
                ?? NoMoves;

            return result;
        }

        private static PolyglotOpeningBook InitializeBook(
            [NotNull] Expression<Func<byte[]>> streamDataGetter)
        {
            PolyglotOpeningBook openingBook;

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var bookName = Factotum.GetPropertyName(streamDataGetter);
            var data = streamDataGetter.Compile().Invoke();

            Trace.WriteLine($"[{currentMethodName}] Initializing the opening book '{bookName}'...");

            var stopwatch = Stopwatch.StartNew();
            using (var stream = new MemoryStream(data))
            {
                openingBook = new PolyglotOpeningBook(stream);
            }

            stopwatch.Stop();

            Trace.WriteLine($@"[{currentMethodName}] The opening book has been initialized in {stopwatch.Elapsed}.");

            return openingBook;
        }

        [DebuggerDisplay("{GetType().Name,nq}: Key = {Key.ToString(\"X16\"),nq}, Move = {Move}, Weight = {Weight}, Learn = {Learn}")]
        private readonly struct BookEntry
        {
            public const int DataLength = 16;

            private static readonly PieceType[] PromotionMapping =
            {
                PieceType.None,
                PieceType.Knight,
                PieceType.Bishop,
                PieceType.Rook,
                PieceType.Queen
            };

            [ThreadStatic]
            private static byte[] _buffer;

            private BookEntry(ulong key, ushort rawMove, ushort weight, uint learn)
            {
                Key = (long)key;
                Move = ParseMove(rawMove);
                Weight = weight;
                Learn = learn;
            }

            public long Key
            {
                get;
            }

            public GameMove Move
            {
                get;
            }

            public ushort Weight
            {
                get;
            }

            public uint Learn
            {
                get;
            }

            public static BookEntry? ReadEntry([NotNull] Stream stream)
            {
                _buffer ??= new byte[DataLength];

                var bytesRead = stream.Read(_buffer, 0, DataLength);
                if (bytesRead == 0)
                {
                    return null;
                }

                if (bytesRead != DataLength)
                {
                    throw new InvalidOperationException(
                        $@"Error reading the stream. Bytes read: {bytesRead}. Bytes required: {DataLength}.");
                }

                ReverseBufferSlotsIfNeeded();

                var offset = 0;
                var key = BitConverter.ToUInt64(_buffer, offset);

                offset += sizeof(ulong);
                var move = BitConverter.ToUInt16(_buffer, offset);

                offset += sizeof(ushort);
                var weight = BitConverter.ToUInt16(_buffer, offset);

                offset += sizeof(ushort);
                var learn = BitConverter.ToUInt32(_buffer, offset);

                var result = new BookEntry(key, move, weight, learn);
                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReverseBufferSlotsIfNeeded()
            {
                if (!BitConverter.IsLittleEndian)
                {
                    return;
                }

                var offset = 0;
                ReverseSingleSlot(ref offset, sizeof(ulong));
                ReverseSingleSlot(ref offset, sizeof(ushort));
                ReverseSingleSlot(ref offset, sizeof(ushort));
                ReverseSingleSlot(ref offset, sizeof(uint));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReverseSingleSlot(ref int offset, int size)
            {
                Array.Reverse(_buffer, offset, size);
                offset += size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static GameMove ParseMove(ushort rawMove)
            {
                var toFile = rawMove & 7;
                var toRow = (rawMove >> 3) & 7;
                var to = new Square(toFile, toRow);

                var fromFile = (rawMove >> 6) & 7;
                var fromRow = (rawMove >> 9) & 7;
                var from = new Square(fromFile, fromRow);

                var rawPromotion = (rawMove >> 12) & 7;
                var promotion = PromotionMapping[rawPromotion];

                var result = new GameMove(from, to, promotion);
                return result;
            }
        }
    }
}