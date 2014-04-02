using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ChessPlatform.Pieces
{
    public abstract class Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Piece"/> class.
        /// </summary>
        internal Piece()
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public PieceColor Color
        {
            get;
            private set;
        }

        public Position Position
        {
            get;
            internal set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", this.Color.GetName(), GetType().Name);
        }

        public char GetFenChar()
        {
            return this.Color == PieceColor.White
                ? char.ToUpperInvariant(this.BaseFenChar)
                : char.ToLowerInvariant(this.BaseFenChar);
        }

        #endregion

        #region Protected Properties

        //// TODO [vmcl] Use attribute instead of abstract property
        protected abstract char BaseFenChar
        {
            get;
        }

        #endregion

        #region Internal Methods

        internal static Piece CreatePiece(Type pieceType, PieceColor color, Position position)
        {
            #region Argument Check

            if (pieceType == null)
            {
                throw new ArgumentNullException("pieceType");
            }

            if (!typeof(Piece).IsAssignableFrom(pieceType))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid piece type '{0}'.",
                        pieceType.GetQualifiedName()),
                    "pieceType");
            }

            color.EnsureDefined();

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            //// TODO [vmcl] Cache constructors, if needed
            var constructorInfo = pieceType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            var result = (Piece)constructorInfo.Invoke(null);
            result.Initialize(color, position);
            return result;
        }

        internal static TPiece CreatePiece<TPiece>(PieceColor color, Position position)
            where TPiece : Piece
        {
            return (TPiece)CreatePiece(typeof(TPiece), color, position);
        }

        #endregion

        #region Private Methods

        private void Initialize(PieceColor color, Position position)
        {
            #region Argument Check

            color.EnsureDefined();

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            this.Color = color;
            this.Position = position;
        }

        #endregion
    }
}