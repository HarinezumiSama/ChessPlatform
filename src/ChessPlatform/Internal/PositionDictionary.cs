using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal sealed class PositionDictionary<TValue> : IDictionary<Position, TValue>
    {
        #region Constants and Fields

        private readonly DictionaryValueHolder<TValue>[] _items;

        #endregion

        #region Constructors

        public PositionDictionary()
            : this(new DictionaryValueHolder<TValue>[ChessConstants.X88Length])
        {
            // Nothing to do
        }

        public PositionDictionary(IEnumerable<KeyValuePair<Position, TValue>> dictionary)
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

        public PositionDictionary(PositionDictionary<TValue> dictionary)
            : this(dictionary.EnsureNotNull()._items.Copy())
        {
            if (_items.Length != ChessConstants.SquareCount)
            {
                throw new InvalidOperationException("Invalid item array length in the source dictionary.");
            }
        }

        private PositionDictionary(DictionaryValueHolder<TValue>[] items)
        {
            _items = items.EnsureNotNull();

            this.Keys = new KeyCollection(this);
            this.Values = new ValueCollection(this);
        }

        #endregion

        #region IDictionary<Position, TValue> Members

        public ICollection<Position> Keys
        {
            get;
            private set;
        }

        public ICollection<TValue> Values
        {
            get;
            private set;
        }

        public TValue this[Position key]
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
                _items[index] = new DictionaryValueHolder<TValue> { IsSet = true, Value = value };
            }
        }

        public void Add(Position key, TValue value)
        {
            var index = GetIndex(key);
            var item = _items[index];
            if (item.IsSet)
            {
                throw new ArgumentException("An element with the same key already exists.", "key");
            }

            _items[index] = new DictionaryValueHolder<TValue> { IsSet = true, Value = value };
        }

        public bool ContainsKey(Position key)
        {
            var index = GetIndex(key);
            var item = _items[index];
            return item.IsSet;
        }

        public bool Remove(Position key)
        {
            var index = GetIndex(key);

            var result = _items[index].IsSet;
            _items[index] = new DictionaryValueHolder<TValue>();
            return result;
        }

        public bool TryGetValue(Position key, out TValue value)
        {
            var index = GetIndex(key);
            var item = _items[index];

            var result = item.IsSet;
            value = result ? item.Value : default(TValue);
            return result;
        }

        #endregion

        #region ICollection<KeyValuePair<Position, TValue>> Members

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

        void ICollection<KeyValuePair<Position, TValue>>.Add(KeyValuePair<Position, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        public void Clear()
        {
            for (var index = 0; index < _items.Length; index++)
            {
                _items[index] = new DictionaryValueHolder<TValue>();
            }
        }

        bool ICollection<KeyValuePair<Position, TValue>>.Contains(KeyValuePair<Position, TValue> pair)
        {
            TValue value;
            return TryGetValue(pair.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, pair.Value);
        }

        void ICollection<KeyValuePair<Position, TValue>>.CopyTo(KeyValuePair<Position, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<Position, TValue>>.Remove(KeyValuePair<Position, TValue> pair)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<Position, TValue>> Members

        public IEnumerator<KeyValuePair<Position, TValue>> GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var position in ChessHelper.AllPositions)
            {
                var index = GetIndex(position);
                var item = _items[index];

                if (item.IsSet)
                {
                    yield return new KeyValuePair<Position, TValue>(position, item.Value);
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

        private static int GetIndex(Position key)
        {
            return key.SquareIndex;
        }

        #endregion

        #region KeyCollection Class

        private sealed class KeyCollection : ICollection<Position>
        {
            #region Constants and Fields

            private readonly PositionDictionary<TValue> _dictionary;

            #endregion

            #region Constructors

            internal KeyCollection(PositionDictionary<TValue> dictionary)
            {
                _dictionary = dictionary.EnsureNotNull();
            }

            #endregion

            #region ICollection<Position> Members

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

            public void Add(Position item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(Position item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(Position[] array, int arrayIndex)
            {
                var index = arrayIndex;
                foreach (var item in this)
                {
                    array[index++] = item;
                }
            }

            public bool Remove(Position item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<Position> Members

            public IEnumerator<Position> GetEnumerator()
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

            private readonly PositionDictionary<TValue> _dictionary;

            #endregion

            #region Constructors

            internal ValueCollection(PositionDictionary<TValue> dictionary)
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