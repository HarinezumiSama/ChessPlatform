using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal sealed class PieceDictionary<TValue> : IDictionary<Piece, TValue>
    {
        #region Constants and Fields

        // ReSharper disable once StaticFieldInGenericType
        private static readonly int MaxIndex = EnumFactotum.GetAllValues<Piece>().Max(item => (int)item);

        private readonly ValueHolder[] _items;

        #endregion

        #region Constructors

        public PieceDictionary()
        {
            _items = new ValueHolder[MaxIndex + 1];

            this.Keys = new KeyCollection(this);
            this.Values = new ValueCollection(this);
        }

        public PieceDictionary(IEnumerable<KeyValuePair<Piece, TValue>> dictionary)
            : this()
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

        public ICollection<Piece> Keys
        {
            get;
            private set;
        }

        public ICollection<TValue> Values
        {
            get;
            private set;
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

        #endregion

        #region ICollection<KeyValuePair<Piece, TValue>> Members

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
            TValue value;
            return TryGetValue(pair.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, pair.Value);
        }

        void ICollection<KeyValuePair<Piece, TValue>>.CopyTo(KeyValuePair<Piece, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<Piece, TValue>>.Remove(KeyValuePair<Piece, TValue> pair)
        {
            throw new NotImplementedException();
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

        #region KeyCollection Class

        private sealed class KeyCollection : ICollection<Piece>
        {
            #region Constants and Fields

            private readonly PieceDictionary<TValue> _dictionary;

            #endregion

            #region Constructors

            internal KeyCollection(PieceDictionary<TValue> dictionary)
            {
                _dictionary = dictionary.EnsureNotNull();
            }

            #endregion

            #region ICollection<Piece> Members

            public int Count
            {
                get
                {
                    return _dictionary.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(Piece item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(Piece item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(Piece[] array, int arrayIndex)
            {
                var index = arrayIndex;
                foreach (var item in this)
                {
                    array[index++] = item;
                }
            }

            public bool Remove(Piece item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<Piece> Members

            public IEnumerator<Piece> GetEnumerator()
            {
                return _dictionary.Select(pair => pair.Key).GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region ValueCollection Class

        private sealed class ValueCollection : ICollection<TValue>
        {
            #region Constants and Fields

            private readonly PieceDictionary<TValue> _dictionary;

            #endregion

            #region Constructors

            internal ValueCollection(PieceDictionary<TValue> dictionary)
            {
                _dictionary = dictionary.EnsureNotNull();
            }

            #endregion

            #region ICollection<TValue> Members

            public int Count
            {
                get
                {
                    return _dictionary.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(TValue item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TValue item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                var index = arrayIndex;
                foreach (var item in this)
                {
                    array[index++] = item;
                }
            }

            public bool Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<TValue> Members

            public IEnumerator<TValue> GetEnumerator()
            {
                return _dictionary.Select(pair => pair.Value).GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion
    }
}