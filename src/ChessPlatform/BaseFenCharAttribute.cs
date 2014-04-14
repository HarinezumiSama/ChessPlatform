using System;
using System.Linq;
using System.Reflection;

namespace ChessPlatform
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class BaseFenCharAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseFenCharAttribute"/> class.
        /// </summary>
        public BaseFenCharAttribute(char baseFenChar)
        {
            #region Argument Check

            if (!char.IsLetter(baseFenChar))
            {
                throw new ArgumentException("The FEN character must be a letter.", "baseFenChar");
            }

            #endregion

            this.BaseFenChar = baseFenChar;
        }

        #endregion

        #region Public Properties

        public char BaseFenChar
        {
            get;
            private set;
        }

        #endregion

        #region Internal Methods

        internal static char GetBaseFenCharNonCached(Enum enumValue)
        {
            var field = enumValue
                .GetType()
                .GetField(enumValue.GetName(), BindingFlags.Static | BindingFlags.Public)
                .EnsureNotNull();

            var attribute = field.GetSingleCustomAttribute<BaseFenCharAttribute>(false);
            return attribute.BaseFenChar;
        }

        #endregion
    }
}