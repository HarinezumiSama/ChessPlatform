using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    internal sealed class PieceData
    {
        #region Constructors

        internal PieceData()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            this.Pieces = new Piece[ChessConstants.X88Length];
            this.PieceOffsetMap = new Dictionary<Piece, HashSet<byte>>();
        }

        private PieceData(PieceData other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            #endregion

            this.Pieces = other.Pieces.Copy();
            this.PieceOffsetMap = ChessHelper.CopyPieceOffsetMap(other.PieceOffsetMap);
        }

        #endregion

        #region Public Properties

        public Piece[] Pieces
        {
            get;
            private set;
        }

        public Dictionary<Piece, HashSet<byte>> PieceOffsetMap
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public PieceData Copy()
        {
            return new PieceData(this);
        }

        public Piece GetPiece(Position position)
        {
            return this.Pieces[position.X88Value];
        }

        public PieceInfo GetPieceInfo(Position position)
        {
            var piece = GetPiece(position);
            return new PieceInfo(piece);
        }

        #endregion
    }
}