using System;
using System.Linq;

namespace ChessPlatform
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
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
    }
}