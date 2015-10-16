using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ChessPlatform.Internal
{
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class FenCharAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="FenCharAttribute"/> class.
        /// </summary>
        public FenCharAttribute(char baseFenChar)
        {
            #region Argument Check

            if (!char.IsLetter(baseFenChar))
            {
                throw new ArgumentException("The FEN character must be a letter.", nameof(baseFenChar));
            }

            #endregion

            this.BaseFenChar = baseFenChar;
        }

        #endregion

        #region Public Properties

        public char BaseFenChar
        {
            get;
        }

        #endregion

        #region Internal Methods

        internal static char? TryGet(FieldInfo enumValueFieldInfo)
        {
            #region Argument Check

            if (enumValueFieldInfo == null)
            {
                throw new ArgumentNullException(nameof(enumValueFieldInfo));
            }

            if (!enumValueFieldInfo.DeclaringType.EnsureNotNull().IsEnum)
            {
                throw new ArgumentException("Invalid field.", nameof(enumValueFieldInfo));
            }

            #endregion

            var attribute = enumValueFieldInfo.GetSingleOrDefaultCustomAttribute<FenCharAttribute>(false);
            return attribute?.BaseFenChar;
        }

        internal static char? TryGet(Enum enumValue)
        {
            var field = enumValue
                .GetType()
                .GetField(enumValue.GetName(), BindingFlags.Static | BindingFlags.Public)
                .EnsureNotNull();

            return TryGet(field);
        }

        internal static char Get(Enum enumValue)
        {
            var intermediateResult = TryGet(enumValue);
            if (!intermediateResult.HasValue)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The enumeration value '{0}' does not have the attribute '{1}' applied.",
                        enumValue,
                        typeof(FenCharAttribute).GetQualifiedName()),
                    nameof(enumValue));
            }

            return intermediateResult.Value;
        }

        #endregion
    }
}