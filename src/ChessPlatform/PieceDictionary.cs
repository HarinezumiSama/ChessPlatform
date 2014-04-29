using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    internal sealed class PieceDictionary<TValue> : IDictionary<Piece, TValue>
    {
        #region Constants and Fields

        // ReSharper disable once StaticFieldInGenericType
        private static readonly int MaxIndex = EnumHelper.GetAllValues<Piece>().Max(item => (int)item);

        private readonly ValueHolder[] _items = new ValueHolder[MaxIndex + 1];

        #endregion

        #region Constructors

        public PieceDictionary()
        {
            // Nothing to do
        }

        public PieceDictionary(IEnumerable<KeyValuePair<Piece, TValue>> dictionary)
        {
            #region Argument Check

            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            #endregion

            foreach (var pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

        #endregion

        #region IDictionary<Piece, TValue> Members

        public void Add(Piece key, TValue value)
        {
            var index = GetIndex(key);
            var item = _items[index];
            if (item.IsSet)
            {
                throw new ArgumentException("An element with the same key already exists.", "key");
            }

            _items[index] = new ValueHolder { IsSet = true, Value = value };
        }

        public bool ContainsKey(Piece key)
        {
            var index = GetIndex(key);
            var item = _items[index];
            return item.IsSet;
        }

        public ICollection<Piece> Keys
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public bool Remove(Piece key)
        {
            var index = GetIndex(key);

            var result = _items[index].IsSet;
            _items[index] = new ValueHolder();
            return result;
        }

        public bool TryGetValue(Piece key, out TValue value)
        {
            var index = GetIndex(key);
            var item = _items[index];

            var result = item.IsSet;
            value = result ? item.Value : default(TValue);
            return result;
        }

        public ICollection<TValue> Values
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public TValue this[Piece key]
        {
            get
            {
                TValue result;
                var found = TryGetValue(key, out result);
                if (!found)
                {
                    throw new KeyNotFoundException();
                }

                return result;
            }

            set
            {
                var index = GetIndex(key);
                _items[index] = new ValueHolder { IsSet = true, Value = value };
            }
        }

        #endregion

        #region ICollection<KeyValuePair<Piece, TValue>> Members

        void ICollection<KeyValuePair<Piece, TValue>>.Add(KeyValuePair<Piece, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        public void Clear()
        {
            for (var index = 0; index < _items.Length; index++)
            {
                _items[index] = new ValueHolder();
            }
        }

        bool ICollection<KeyValuePair<Piece, TValue>>.Contains(KeyValuePair<Piece, TValue> pair)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<Piece, TValue>>.CopyTo(KeyValuePair<Piece, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get
            {
                return _items.Count(item => item.IsSet);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<Piece, TValue>>.Remove(KeyValuePair<Piece, TValue> item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<Piece,TValue>> Members

        public IEnumerator<KeyValuePair<Piece, TValue>> GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var piece in ChessConstants.Pieces)
            {
                var index = GetIndex(piece);
                var item = _items[index];

                if (item.IsSet)
                {
                    yield return new KeyValuePair<Piece, TValue>(piece, item.Value);
                }
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private Methods

        private static int GetIndex(Piece key)
        {
            return (int)key;
        }

        #endregion

        #region ValueHolder Structure

        private struct ValueHolder
        {
            #region Public Properties

            public bool IsSet
            {
                get;
                set;
            }

            public TValue Value
            {
                get;
                set;
            }

            #endregion
        }

        #endregion
    }
}