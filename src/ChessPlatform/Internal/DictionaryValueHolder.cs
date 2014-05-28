using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct DictionaryValueHolder<TValue>
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
}