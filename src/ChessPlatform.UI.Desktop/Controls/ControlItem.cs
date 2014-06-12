using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.Controls
{
    internal sealed class ControlItem<T> : IEquatable<ControlItem<T>>
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ControlItem{T}"/> class
        ///     using the specified value and text.
        /// </summary>
        public ControlItem([CanBeNull] T value, [CanBeNull] string text)
        {
            this.Value = value;
            this.Text = text ?? value.ToStringSafelyInvariant();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ControlItem{T}"/> class
        ///     using the specified value.
        /// </summary>
        public ControlItem([CanBeNull] T value)
            : this(value, null)
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public T Value
        {
            get;
            private set;
        }

        [NotNull]
        public string Text
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as ControlItem<T>);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCodeSafely();
        }

        public override string ToString()
        {
            return this.Text;
        }

        #endregion

        #region IEquatable<ControlItem<T>> Members

        public bool Equals(ControlItem<T> other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(this.Value, other.Value);
        }

        #endregion
    }
}