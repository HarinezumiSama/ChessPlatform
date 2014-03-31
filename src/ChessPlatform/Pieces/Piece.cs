using System;
using System.Globalization;
using System.Linq;

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

        public static Piece CreatePiece(Type pieceType, PieceColor color, Position position)
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

            var result = (Piece)Activator.CreateInstance(pieceType);
            result.Initialize(color, position);
            return result;
        }

        #endregion

        #region Internal Methods

        internal void Initialize(PieceColor color, Position position)
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