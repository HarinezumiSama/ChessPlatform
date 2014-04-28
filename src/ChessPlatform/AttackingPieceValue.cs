using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    internal struct AttackingPieceValue
    {
        #region Constants and Fields

        private readonly Piece _piece;
        private readonly Position[] _positions;

        #endregion

        #region Constructors

        public AttackingPieceValue(Piece piece, Position[] positions)
        {
            #region Constants and Fields

            piece.EnsureDefined();

            if (piece == Piece.None)
            {
                throw new ArgumentException("Cannot be an empty square.", "piece");
            }

            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }


            #endregion

            _piece = piece;
            _positions = positions;
        }

        #endregion

        #region Public Properties

        public Piece Piece
        {
            [DebuggerStepThrough]
            get
            {
                return _piece;
            }
        }

        public Position[] Positions
        {
            [DebuggerStepThrough]
            get
            {
                return _positions;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{{0}, {1}}}",
                _piece.GetDescription(),
                _positions.Length);
        }

        #endregion
    }
}