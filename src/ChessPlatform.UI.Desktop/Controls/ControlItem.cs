using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.Controls
{
    internal sealed class ControlItem<T> : IEquatable<ControlItem<T>>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ControlItem{T}"/> class
        ///     using the specified value and text.
        /// </summary>
        public ControlItem([CanBeNull] T value, [CanBeNull] string text)
        {
            Value = value;
            Text = text ?? value.ToStringSafelyInvariant();
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

        [CanBeNull]
        public T Value
        {
            get;
        }

        [NotNull]
        public string Text
        {
            get;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ControlItem<T>);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCodeSafely();
        }

        public override string ToString()
        {
            return Text;
        }

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

            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }
    }
}